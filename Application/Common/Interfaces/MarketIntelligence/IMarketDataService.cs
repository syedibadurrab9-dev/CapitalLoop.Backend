using Application.AiAdvisor.Queries.GetAssetAllocationDeficiencies;
using Application.Portfolios.Queries;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.MarketIntelligence;

/// <summary>
/// Core Financial Market Data Engine Provider Contract [CL-201 / CL-203 / CL-206].
/// Normalizes disparate pricing telemetry data points across distinct asset blocks 
/// (PSX Equities, Binance Crypto, Al Meezan Mutual Funds, and physical precious metals)
/// down to high-precision, localized valuation metrics for portfolio calculation engines.
/// </summary>
public interface IMarketDataService
{
    // =============================================================================
    // 📊 MASTER REBALANCING & FORECAST ENGINE METHODS (CQRS INTEGRATION)
    // =============================================================================

    /// <summary>
    /// Provides a high-speed, uniform spot price lookup metric for any supported token or stock symbol.
    /// Natively utilized by GetAggregatedNetWorthQuery.
    /// </summary>
    /// <param name="ticker">The uppercase symbol target (e.g., "SYS", "BTC", "SILVER").</param>
    /// <param name="cancellationToken">Propagates notification that operational threads should be canceled.</param>
    Task<decimal> GetLiveSpotPriceAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gauges variance drifts against target allocations and evaluates Shariah-compliant asset limits.
    /// Natively utilized by GetAssetAllocationDeficienciesQuery.
    /// </summary>
    /// <param name="profileId">The unique root aggregate identifier of the targeted UserProfile.</param>
    /// <returns>A calculation snapshot mapping target discrepancies, drift vectors, and compliance flags.</returns>
    Task<AllocationAnalysisDto> AnalyzeDriftVectorsAsync(Guid profileId);

    /// <summary>
    /// Simulates compound wealth growth curves over extended timelines based on risk weights.
    /// Natively utilized by GetForecastProjectionsQuery.
    /// </summary>
    /// <param name="riskScore">The computed weighting integer extracted from suitability models.</param>
    /// <param name="horizonMonths">The projection window duration bound (e.g., 36 months to 120 months).</param>
    /// <returns>A mathematical trend trajectory array modeling optimal vs conservative yield outcomes.</returns>
    Task<ForecastProjectionsDto> SimulateGrowthAsync(int riskScore, int horizonMonths);

    // =============================================================================
    // 🪙 PHYSICAL COMMODITIES TELEMETRY INTERFACES (GOLD / SILVER)
    // =============================================================================

    /// <summary>
    /// Fetches the live spot market price per traditional Tola for physical Gold and Silver bullion [CL-203].
    /// </summary>
    Task<PreciousMetalsPricePayload> GetPreciousMetalsSpotPricesAsync(bool bypassCache = false, CancellationToken cancellationToken = default);

    // =============================================================================
    // 💱 FOREIGN EXCHANGE TELEMETRY INTERFACES (FOREX)
    // =============================================================================

    /// <summary>
    /// Retrieves the live spot market exchange price for USD to PKR conversion metrics [CL-201].
    /// </summary>
    Task<decimal> GetLiveLiveUsdPkrRateAsync(bool bypassCache = false, CancellationToken cancellationToken = default);

    // =============================================================================
    // 📈 CAPITAL MARKETS & ASSET MANAGEMENT INTERFACES (PSX / MUTUAL FUNDS)
    // =============================================================================

    /// <summary>
    /// Fetches pricing metrics for a given equity symbol listed on the Pakistan Stock Exchange (PSX) [CL-206].
    /// </summary>
    Task<decimal> GetPsxStockPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// High-Performance Batch Optimization Engine.
    /// Fetches pricing metrics concurrently for a matrix collection of Pakistan Stock Exchange tickers.
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetBatchPsxStockPricesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls the current Net Asset Value (NAV) pricing index per unit for open-ended funds [CL-206].
    /// </summary>
    Task<decimal> GetMutualFundNavAsync(string fundCode, CancellationToken cancellationToken = default);
}

// =============================================================================
// RECORD PARAMETER DATA CONTRACTS (IMMUTABLE TELEMETRY WIRE MODELS)
// =============================================================================

/// <summary>
/// Strongly-typed domain wrapper capturing physical precious metals valuation parameters [CL-203].
/// </summary>
public record PreciousMetalsPricePayload
{
    public decimal GoldPricePerTolaPkr { get; init; }
    public decimal SilverPricePerTolaPkr { get; init; }
    public DateTime LastFetchedUtc { get; init; }
    public bool IsStaleData { get; init; }

    public PreciousMetalsPricePayload(decimal goldPrice, decimal silverPrice, DateTime lastFetchedUtc, bool isStaleData = false)
    {
        if (goldPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(goldPrice), "Commodity contract error: Incoming market gold price must represent a positive non-zero sum.");

        if (silverPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(silverPrice), "Commodity contract error: Incoming market silver price must represent a positive non-zero sum.");

        GoldPricePerTolaPkr = goldPrice;
        SilverPricePerTolaPkr = silverPrice;
        LastFetchedUtc = lastFetchedUtc == DateTime.MinValue ? DateTime.UtcNow : lastFetchedUtc;
        IsStaleData = isStaleData;
    }
}