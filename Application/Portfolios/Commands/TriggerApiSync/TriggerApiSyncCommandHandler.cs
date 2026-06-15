using Application.Common.Interfaces;
using Application.Common.Interfaces.ExternalIntegrations;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Commands.TriggerApiSync;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Intercepts API synchronization events to fetch real-time valuations across multiple external exchanges 
/// and update user asset position node ledgers cleanly [CL-405 / CL-414].
/// </summary>
public class TriggerApiSyncCommandHandler : IRequestHandler<TriggerApiSyncCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICryptoSyncService _cryptoSyncService;
    private readonly IAkdBrokerageSyncService _brokerageSyncService;
    private readonly IPreciousMetalSyncService _preciousMetalSyncService;
    private readonly IPsxScraperService _psxScraperService;
    private readonly ILogger<TriggerApiSyncCommandHandler> _logger;

    public TriggerApiSyncCommandHandler(
        IApplicationDbContext context,
        ICryptoSyncService cryptoSyncService,
        IAkdBrokerageSyncService brokerageSyncService,
        IPreciousMetalSyncService preciousMetalSyncService,
        IPsxScraperService psxScraperService,
        ILogger<TriggerApiSyncCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cryptoSyncService = cryptoSyncService ?? throw new ArgumentNullException(nameof(cryptoSyncService));
        _brokerageSyncService = brokerageSyncService ?? throw new ArgumentNullException(nameof(brokerageSyncService));
        _preciousMetalSyncService = preciousMetalSyncService ?? throw new ArgumentNullException(nameof(preciousMetalSyncService));
        _psxScraperService = psxScraperService ?? throw new ArgumentNullException(nameof(psxScraperService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(TriggerApiSyncCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Executing Comprehensive Live Sync Pipeline | Target Asset Node: {AssetId} for Profile: {Email}",
            command.AssetNodeId, sanitizedEmail);

        // 🎯 PERSISTENCE REALIGNMENT: Fetch the target asset node out of your realigned database table context
        var assetNode = await _context.AssetNodes
            .FirstOrDefaultAsync(a => a.Id == command.AssetNodeId && a.UserEmail == sanitizedEmail, cancellationToken);

        if (assetNode is null)
            throw new InvalidOperationException($"Sync Execution Rejected: Target asset tracker index '{command.AssetNodeId}' does not exist or access is restricted.");

        // 🛡️ DOMAIN INVARIANT GUARDRAIL: Block processing loops if the asset node doesn't leverage automated credentials
        if (!assetNode.IsAutoSynced || string.IsNullOrWhiteSpace(assetNode.EncryptedApiKey))
            throw new InvalidOperationException("Sync Execution Aborted: Targeted asset position node is configured as a manual ledger and cannot process external automation hooks.");

        decimal freshCalculatedValuationPkr = 0;

        // =============================================================================
        // 🔀 MULTI-CLASS MATRIX ROUTING ROUTINES
        // =============================================================================
        // 🧱 FIXED CS0117: Synchronized case mapping branches to follow your true system Enum names precisely
        switch (assetNode.Category)
        {
            // 🪙 METRIC BRANCH A: CRYPTOCURRENCY BALANCES (Binance Cross-Wallet Exchange Sync)
            case AssetCategory.Crypto:
                _logger.LogInformation("Routing crypto aggregation tracks for wallet node: {AssetId}", assetNode.Id);
                freshCalculatedValuationPkr = await _cryptoSyncService.FetchLiveWalletBalanceAsync(assetNode.EncryptedApiKey, cancellationToken);
                break;

            // 💼 METRIC BRANCH B: EQUITIES PARSED VIA INTEGRATED BROKERAGE LEDGERS (AKD Statements Engine)
            case AssetCategory.Brokerage:
                _logger.LogInformation("Routing traditional brokerage statement ingestion tracks for node: {AssetId}", assetNode.Id);
                freshCalculatedValuationPkr = await _brokerageSyncService.FetchBrokerageBalanceAsync(sanitizedEmail, cancellationToken);
                break;

            // 🪙 METRIC BRANCH C: PHYSICAL PRECIOUS METALS (Throttled Regional Bullion Spot Feeds)
            case AssetCategory.PreciousMetal:
                _logger.LogInformation("Routing physical commodities spot tracking loops for node: {AssetId}", assetNode.Id);
                // Uses the entity's underlying symbol reference (e.g., "SILVER10TOLA" or "GOLD24K") to extract spot rates per Tola
                decimal spotRatePerTola = await _preciousMetalSyncService.FetchSpotRatePerTolaAsync(assetNode.Symbol ?? "GOLD24K", cancellationToken);
                decimal aggregateTolasCount = assetNode.QuantityHoldings > 0 ? assetNode.QuantityHoldings : 1.00m;
                freshCalculatedValuationPkr = spotRatePerTola * aggregateTolasCount;
                break;

            // 📈 METRIC BRANCH D: INDIVIDUAL BLUE-CHIP STOCKS (Throttled Parallel PSX Crawler Runtimes)
            // 🧱 FIXED CS0117: Explicitly updated switch target from 'Equities' to 'PsxEquity' matching your enum signature
            case AssetCategory.PsxEquity:
                _logger.LogInformation("Routing active equity exchange board scraping sweep for node: {AssetId}", assetNode.Id);
                decimal closingTickerPrice = await _psxScraperService.FetchEquityClosingPriceAsync(assetNode.Symbol ?? "SYS", cancellationToken);
                decimal shareVolumeOwned = assetNode.QuantityHoldings > 0 ? assetNode.QuantityHoldings : 0.00m;
                freshCalculatedValuationPkr = closingTickerPrice * shareVolumeOwned;
                break;

            default:
                throw new NotImplementedException($"Automated synchronization patterns for asset category classification '{assetNode.Category}' are unmapped within this solution branch.");
        }

        if (freshCalculatedValuationPkr < 0)
        {
            _logger.LogWarning("Live Sync Defeated | Downstream interface pipelines returned uncalculated balances for node {AssetId}", assetNode.Id);
            return false;
        }

        // =============================================================================
        // 🧱 AGGREGATE STATE HYDRATION & PERSISTENCE
        // =============================================================================
        // 🧱 FIXED CS7036: Added 'sanitizedEmail' as the third tracking argument to satisfy the signature validation layout
        assetNode.HydrateLiveBalanceFromApi(freshCalculatedValuationPkr, assetNode.EncryptedApiKey, sanitizedEmail);

        // Save changes back down to the relational storage context
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Live Sync Success | Asset node index {AssetId} updated safely. New Position Valuation: {CurrentVal} PKR",
            assetNode.Id, freshCalculatedValuationPkr);

        return true;
    }
}