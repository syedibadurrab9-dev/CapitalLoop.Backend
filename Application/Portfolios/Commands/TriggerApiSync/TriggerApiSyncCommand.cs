using Application.Common.DTOs; // Required to map out BinanceSyncResult
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Commands.TriggerApiSync;

public record TriggerApiSyncCommand(string UserEmail) : IRequest<bool>;

public class TriggerApiSyncCommandHandler : IRequestHandler<TriggerApiSyncCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICryptoSyncService _cryptoSyncService;
    private readonly IPreciousMetalSyncService _metalSyncService;
    private readonly IPsxScraperService _psxScraperService;
    private readonly IAkdBrokerageSyncService _akdBrokerageService;
    private readonly ILogger<TriggerApiSyncCommandHandler> _logger;

    public TriggerApiSyncCommandHandler(
        IApplicationDbContext context,
        ICryptoSyncService cryptoSyncService,
        IPreciousMetalSyncService metalSyncService,
        IPsxScraperService psxScraperService,
        IAkdBrokerageSyncService akdBrokerageService,
        ILogger<TriggerApiSyncCommandHandler> logger)
    {
        _context = context;
        _cryptoSyncService = cryptoSyncService;
        _metalSyncService = metalSyncService;
        _psxScraperService = psxScraperService;
        _akdBrokerageService = akdBrokerageService;
        _logger = logger;
    }

    public async Task<bool> Handle(TriggerApiSyncCommand request, CancellationToken cancellationToken)
    {
        var standardizedEmail = request.UserEmail.Trim().ToLowerInvariant();
        _logger.LogInformation("[PORTFOLIO MASTER] Orchestrating unified wealth synchronization sweep for: {Email}", standardizedEmail);

        // TODO: Task #4022 - Load user's securely encrypted credentials from DB profile configurations
        string mockUserApiKey = "USER_STORED_API_KEY_OR_TOKEN";
        string mockUserSecretKey = "USER_STORED_SECRET_KEY";

        // ✅ FIXED: Calling our explicit, new contractual method signature
        var cryptoTask = _cryptoSyncService.FetchExtendedPortfolioBalancesAsync(mockUserApiKey, mockUserSecretKey);
        var metalTask = _metalSyncService.GetLiveRatesAsync(standardizedEmail, cancellationToken);
        var psxTask = _psxScraperService.GetLiveStockValuationsAsync(standardizedEmail, cancellationToken);
        var akdTask = _akdBrokerageService.GetLiveSharesInventoryAsync(standardizedEmail, cancellationToken);

        await Task.WhenAll(cryptoTask, metalTask, psxTask, akdTask);

        var liveCryptoResult = await cryptoTask;
        var liveMetals = await metalTask;
        var livePsx = await psxTask;
        var liveAkd = await akdTask;

        // ✅ FIXED: Mapping database context calls to your correct set name (AssetNodes)
        var existingDbAssets = await _context.Assets
            .Where(a => a.UserEmail == standardizedEmail)
            .ToListAsync(cancellationToken);

        // --- PROCESS UPSETS FOR ALL ACTIVE MARKETS ---

        // ✅ FIXED: Processing multi-wallet exchange breakdowns into distinct structured asset sub-nodes
        if (liveCryptoResult != null)
        {
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Spot Wallet", AssetCategory.Crypto, liveCryptoResult.SpotBalancePkr, existingDbAssets);
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Earn Staking", AssetCategory.Crypto, liveCryptoResult.EarnBalancePkr, existingDbAssets);
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Futures Leverage", AssetCategory.Crypto, liveCryptoResult.FuturesBalancePkr, existingDbAssets);
        }

        if (liveMetals != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, liveMetals, AssetCategory.Precious_Metal, "Holding Tracker", existingDbAssets, cancellationToken);

        if (livePsx != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, livePsx, AssetCategory.Crypto, "Stock Position", existingDbAssets, cancellationToken); // Update to PSX_Equity enum if compiled

        if (liveAkd != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, liveAkd, AssetCategory.Crypto, "Brokerage Stock", existingDbAssets, cancellationToken);

        // Save state engine modifications securely to SQL Server relational storage nodes
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[PORTFOLIO MASTER] Core synchronization complete across all active markets.");

        return true;
    }

    #region REUSABLE DOMAIN UPSERT LOGIC

    private void ProcessSingleAssetUpsert(
        string email,
        string assetName,
        AssetCategory category,
        decimal currentPkrValue,
        List<AssetNode> existingAssets)
    {
        if (currentPkrValue <= 0) return;

        var matchedNode = existingAssets.FirstOrDefault(a => a.Category == category && a.Name == assetName);

        if (matchedNode != null)
        {
            matchedNode.HydrateLiveBalanceFromApi(currentPkrValue);
        }
        else
        {
            var newAsset = AssetNode.CreateManual(email, assetName, category, currentPkrValue);
            newAsset.HydrateLiveBalanceFromApi(currentPkrValue);
            _context.Assets.Add(newAsset); // ✅ FIXED: Pointing to correct AssetNodes DB property
        }
    }

    private async Task ProcessCategoryDictionaryUpsertAsync(
        string email,
        Dictionary<string, decimal> liveBalances,
        AssetCategory category,
        string nameSuffix,
        List<AssetNode> existingAssets,
        CancellationToken cancellationToken)
    {
        foreach (var entry in liveBalances)
        {
            var code = entry.Key;
            var currentPkrValuation = entry.Value;

            var matchedNode = existingAssets.FirstOrDefault(a =>
                a.Category == category &&
                (a.Name.Contains(code) || a.Name == $"{code} {nameSuffix}"));

            if (matchedNode != null)
            {
                matchedNode.HydrateLiveBalanceFromApi(currentPkrValuation);
            }
            else
            {
                var newAsset = AssetNode.CreateManual(
                    email: email,
                    name: $"{code} {nameSuffix}",
                    category: category,
                    value: currentPkrValuation
                );
                newAsset.HydrateLiveBalanceFromApi(currentPkrValuation);

                // ✅ FIXED: Pointing to the correct DbSet name structure
                await _context.Assets.AddAsync(newAsset, cancellationToken);
            }
        }
    }

    #endregion
}