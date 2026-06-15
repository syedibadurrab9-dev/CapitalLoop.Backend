using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.ExternalIntegrations;

/// <summary>
/// 📊 DOMAIN INFRASTRUCTURE SCRAPER ENGINE CONTRACT
/// Orchestrates background headless web node routines, direct portal fetches, and high-speed local 
/// Redis cache-aside pipelines to extract equity evaluations directly from the exchange board [CL-206].
/// </summary>
public interface IPsxScraperService
{
    /// <summary>
    /// Extracts a single equity asset's closing price. Utilizes local cache-aside optimization layers 
    /// before resorting to an outbound network scrape event.
    /// Natively utilized by TriggerApiSyncCommand to automate updates for manual or synced stock nodes.
    /// </summary>
    /// <param name="ticker">The uppercase stock ticker symbol (e.g., "SYS", "ENGRO").</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The real-time market price of the security in PKR.</returns>
    Task<decimal> FetchEquityClosingPriceAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls macro-level market movements and index tracking states down into local storage tables.
    /// Natively called by SyncTaxFilerStatusCommand and background tax alignment components.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>True if the local market indices and filer caches are updated successfully; otherwise, false.</returns>
    Task<bool> RefreshLocalPsxIndexCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a thread-isolated scraping routine across the live equity boards to capture 
    /// active market positioning metrics for portfolio alignment calculations.
    /// Evaluates existing cached tokens first to eliminate redundant outbound network I/O bounds.
    /// </summary>
    /// <param name="trackedSymbols">The explicit list of uppercase stock tickers (e.g., "SYS", "ENGRO") to extract.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>A read-only collection of parsed live equity data points.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown instantly if a target data element structural layout shifts.</exception>
    Task<IReadOnlyCollection<PsxScraperPayload>> GetLiveStockValuationsAsync(
        IEnumerable<string> trackedSymbols,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly typed context snapshot holding parsed equity tracking parameters extracted from the live board [CL-206].
/// </summary>
public record PsxScraperPayload(
    string TickerSymbol,
    decimal CurrentMarketPricePkr,
    decimal DailyChangePkr,
    decimal PercentageChange,
    long TradedVolume,
    DateTime LastScrapedUtc
);