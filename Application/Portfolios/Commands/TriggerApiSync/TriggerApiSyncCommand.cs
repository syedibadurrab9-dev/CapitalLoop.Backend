using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Commands.TriggerApiSync;

public record TriggerApiSyncCommand(string UserEmail) : ICommand<bool>;

public class TriggerApiSyncCommandHandler : ICommandHandler<TriggerApiSyncCommand, bool>
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

    public async Task<bool> HandleAsync(TriggerApiSyncCommand command, CancellationToken cancellationToken)
    {
        var standardizedEmail = command.UserEmail.Trim().ToLowerInvariant();
        _logger.LogInformation("[PORTFOLIO MASTER] Orchestrating unified wealth synchronization sweep for: {Email}", standardizedEmail);

        string mockUserApiKey = "USER_STORED_API_KEY_OR_TOKEN";
        string mockUserSecretKey = "USER_STORED_SECRET_KEY";

        var cryptoTask = _cryptoSyncService.FetchExtendedPortfolioBalancesAsync(mockUserApiKey, mockUserSecretKey);
        var metalTask = _metalSyncService.GetLiveRatesAsync(standardizedEmail, cancellationToken);
        var psxTask = _psxScraperService.GetLiveStockValuationsAsync(standardizedEmail, cancellationToken);
        var akdTask = _akdBrokerageService.GetLiveSharesInventoryAsync(standardizedEmail, cancellationToken);

        await Task.WhenAll(cryptoTask, metalTask, psxTask, akdTask);

        var liveCryptoResult = await cryptoTask;
        var liveMetals = await metalTask;
        var livePsx = await psxTask;
        var liveAkd = await akdTask;

        // Fetch existing user assets in a single indexed query (Zero N+1)
        var existingDbAssets = await _context.Assets
            .Where(a => a.UserEmail == standardizedEmail)
            .ToListAsync(cancellationToken);

        // --- PROCESS UPSERTS FOR ALL ACTIVE MARKETS (O(N + M) Algorithmic Complexity) ---

        if (liveCryptoResult != null)
        {
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Spot Wallet", AssetCategory.Crypto, liveCryptoResult.SpotBalancePkr, existingDbAssets);
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Earn Staking", AssetCategory.Crypto, liveCryptoResult.EarnBalancePkr, existingDbAssets);
            ProcessSingleAssetUpsert(standardizedEmail, "Binance Futures Leverage", AssetCategory.Crypto, liveCryptoResult.FuturesBalancePkr, existingDbAssets);
        }

        if (liveMetals != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, liveMetals, AssetCategory.Precious_Metal, "Holding Tracker", existingDbAssets, cancellationToken);

        if (livePsx != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, livePsx, AssetCategory.PSX_Equity, "Stock Position", existingDbAssets, cancellationToken);

        if (liveAkd != null)
            await ProcessCategoryDictionaryUpsertAsync(standardizedEmail, liveAkd, AssetCategory.PSX_Equity, "Brokerage Stock", existingDbAssets, cancellationToken);

        // Save state engine modifications atomically in a single round-trip
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[PORTFOLIO MASTER] Core synchronization complete across all active markets.");

        return true;
    }

    #region REUSABLE DOMAIN UPSERT LOGIC (O(N+M) Complexity, Batch DB Writes)

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
            existingAssets.Add(newAsset);
            _context.Assets.Add(newAsset);
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
        // O(N) pre-index map by asset name for O(1) key lookups
        var categoryMap = existingAssets
            .Where(a => a.Category == category)
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var newAssetsToBatch = new List<AssetNode>();

        foreach (var (code, currentPkrValuation) in liveBalances)
        {
            if (currentPkrValuation <= 0) continue;

            var candidateName = $"{code} {nameSuffix}";
            AssetNode? matchedNode = null;

            if (categoryMap.TryGetValue(candidateName, out var exactMatch))
            {
                matchedNode = exactMatch;
            }
            else
            {
                matchedNode = categoryMap.Values.FirstOrDefault(a => a.Name.Contains(code, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedNode != null)
            {
                matchedNode.HydrateLiveBalanceFromApi(currentPkrValuation);
            }
            else
            {
                var newAsset = AssetNode.CreateManual(
                    email: email,
                    name: candidateName,
                    category: category,
                    value: currentPkrValuation
                );
                newAsset.HydrateLiveBalanceFromApi(currentPkrValuation);

                newAssetsToBatch.Add(newAsset);
                categoryMap[candidateName] = newAsset;
                existingAssets.Add(newAsset);
            }
        }

        if (newAssetsToBatch.Count > 0)
        {
            await _context.Assets.AddRangeAsync(newAssetsToBatch, cancellationToken);
        }
    }

    #endregion
}