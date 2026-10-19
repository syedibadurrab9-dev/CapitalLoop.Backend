using Application.Portfolios.Queries.GetForecastProjections;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.UnitTests;

public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AssetNode> Assets => Set<AssetNode>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<ComplianceAuditLog> ComplianceAuditLogs => Set<ComplianceAuditLog>();
    public DbSet<AdminOperator> AdminOperators => Set<AdminOperator>();
}

public class ForecastProjectionsTests
{
    private TestDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ActiveFiler_ShouldApplyFilerTaxRatesAndShowTaxSavings()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var email = "filer@capitalloop.pk";

        var user = UserProfile.Register("Tax Filer", email);
        user.UpdateFilerStatus(true);
        context.UserProfiles.Add(user);

        // Assets: PSX 5M PKR, Mutual Fund 5M PKR (Total: 10M PKR)
        var psxAsset = AssetNode.CreateManual(email, "Engro PSX Shares", AssetCategory.PSX_Equity, 5_000_000m);
        var mfAsset = AssetNode.CreateManual(email, "Meezan Islamic Fund", AssetCategory.Mutual_Fund, 5_000_000m);
        context.Assets.AddRange(psxAsset, mfAsset);
        await context.SaveChangesAsync();

        var handler = new GetForecastProjectionsQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetForecastProjectionsQuery(email), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFiler.Should().BeTrue();
        result.CurrentTotalNetWorthPKR.Should().Be(10_000_000m);

        // Filer effective return is higher than non-filer
        result.BlendedEffectiveAnnualReturn.Should().BeGreaterThan(0.12m);
        result.AssetBreakdown.Should().HaveCount(2);

        // Verify Filer tax rates (15% for PSX & Mutual Funds)
        result.AssetBreakdown.Should().AllSatisfy(a =>
        {
            a.FilerTaxRate.Should().Be(0.15m);
            a.NonFilerTaxRate.Should().Be(0.30m);
        });

        // 36-month and 120-month forecasts
        result.Forecast36MonthsPKR.Should().BeGreaterThan(result.CurrentTotalNetWorthPKR);
        result.Forecast120MonthsPKR.Should().BeGreaterThan(result.Forecast36MonthsPKR);

        // Tax savings must be positive for Filers
        result.TaxSavings36MonthsPKR.Should().BeGreaterThan(0m);
        result.TaxSavings120MonthsPKR.Should().BeGreaterThan(result.TaxSavings36MonthsPKR);

        // FBR advisory note confirms active filer
        result.FbrAdvisoryNote.Should().Contain("Verified FBR Active Taxpayer");
    }

    [Fact]
    public async Task HandleAsync_NonFiler_ShouldApplyPunitiveWithholdingAndWarning()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var email = "nonfiler@capitalloop.pk";

        var user = UserProfile.Register("Non Filer", email);
        user.UpdateFilerStatus(false);
        context.UserProfiles.Add(user);

        // Assets: PSX 5M PKR, Mutual Fund 5M PKR (Total: 10M PKR)
        var psxAsset = AssetNode.CreateManual(email, "OGDC Shares", AssetCategory.PSX_Equity, 5_000_000m);
        var mfAsset = AssetNode.CreateManual(email, "UBL Income Fund", AssetCategory.Mutual_Fund, 5_000_000m);
        context.Assets.AddRange(psxAsset, mfAsset);
        await context.SaveChangesAsync();

        var handler = new GetForecastProjectionsQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetForecastProjectionsQuery(email), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFiler.Should().BeFalse();
        result.CurrentTotalNetWorthPKR.Should().Be(10_000_000m);

        // Cumulative tax saved is 0 for non-filer
        var m36 = result.Milestones.First(m => m.Month == 36);
        m36.CumulativeTaxSavedPKR.Should().Be(0m);
        m36.TaxDragPKR.Should().BeGreaterThan(0m);

        // Advisory note flags punitive withholding warning
        result.FbrAdvisoryNote.Should().Contain("ACTION REQUIRED: Unverified or Non-Filer status detected");
    }

    [Fact]
    public async Task HandleAsync_EmptyPortfolio_ShouldReturnDefaultProjectionsGracefully()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var email = "newuser@capitalloop.pk";

        var user = UserProfile.Register("New Investor", email);
        context.UserProfiles.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetForecastProjectionsQueryHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetForecastProjectionsQuery(email), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CurrentTotalNetWorthPKR.Should().Be(0m);
        result.AssetBreakdown.Should().BeEmpty();
        result.Milestones.Should().HaveCount(5);
        result.Forecast36MonthsPKR.Should().BeGreaterThan(0m);
    }
}
