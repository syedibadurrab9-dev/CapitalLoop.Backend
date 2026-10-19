using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.TriggerApiSync;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.UnitTests;

public class SyncServicesTests
{
    private TestDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ShouldSyncAllMarketsConcurrentlyAndUpsertAssets()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var email = "investor@capitalloop.pk";

        var user = UserProfile.Register("Test Investor", email);
        context.UserProfiles.Add(user);

        // Pre-existing asset
        var existingAsset = AssetNode.CreateManual(email, "Binance Spot Wallet", AssetCategory.Crypto, 500_000m);
        context.Assets.Add(existingAsset);
        await context.SaveChangesAsync();

        var mockCrypto = new Mock<ICryptoSyncService>();
        mockCrypto.Setup(c => c.FetchExtendedPortfolioBalancesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new BinanceSyncResult { SpotBalancePkr = 1_200_000m, EarnBalancePkr = 300_000m, FuturesBalancePkr = 0m });

        var mockMetals = new Mock<IPreciousMetalSyncService>();
        mockMetals.Setup(m => m.GetLiveRatesAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                ["Gold Tola 24K"] = 275_000m,
                ["Silver Tola"] = 3_200m
            });

        var mockPsx = new Mock<IPsxScraperService>();
        mockPsx.Setup(p => p.GetLiveStockValuationsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                ["SYS"] = 465_000m,
                ["ENGRO"] = 320_000m
            });

        var mockAkd = new Mock<IAkdBrokerageSyncService>();
        mockAkd.Setup(a => a.GetLiveSharesInventoryAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, decimal>
            {
                ["HUBC"] = 240_000m
            });

        var mockLogger = new Mock<ILogger<TriggerApiSyncCommandHandler>>();

        var handler = new TriggerApiSyncCommandHandler(
            context,
            mockCrypto.Object,
            mockMetals.Object,
            mockPsx.Object,
            mockAkd.Object,
            mockLogger.Object
        );

        // Act
        var result = await handler.HandleAsync(new TriggerApiSyncCommand(email), CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedAssets = await context.Assets.Where(a => a.UserEmail == email).ToListAsync();
        updatedAssets.Should().NotBeEmpty();

        // Binance Spot Wallet updated from 500k to 1.2M
        var spotWallet = updatedAssets.FirstOrDefault(a => a.Name == "Binance Spot Wallet");
        spotWallet.Should().NotBeNull();
        spotWallet!.HoldingValuePKR.Should().Be(1_200_000m);
        spotWallet.IsAutoSynced.Should().BeTrue();

        // PSX Assets synced
        updatedAssets.Should().Contain(a => a.Category == AssetCategory.PSX_Equity && a.Name.Contains("SYS"));
        updatedAssets.Should().Contain(a => a.Category == AssetCategory.PSX_Equity && a.Name.Contains("ENGRO"));
        updatedAssets.Should().Contain(a => a.Category == AssetCategory.PSX_Equity && a.Name.Contains("HUBC"));

        // Metals synced
        updatedAssets.Should().Contain(a => a.Category == AssetCategory.Precious_Metal && a.Name.Contains("Gold"));

        // Verify concurrent executions
        mockCrypto.Verify(c => c.FetchExtendedPortfolioBalancesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        mockMetals.Verify(m => m.GetLiveRatesAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        mockPsx.Verify(p => p.GetLiveStockValuationsAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        mockAkd.Verify(a => a.GetLiveSharesInventoryAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
}
