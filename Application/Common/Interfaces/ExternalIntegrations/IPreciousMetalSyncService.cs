using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.ExternalIntegrations;

/// <summary>
/// Domain infrastructure contract designed to handle localized spot commodity parsing 
/// and inventory valuation mechanics across traditional physical metal holdings [CL-203].
/// </summary>
public interface IPreciousMetalSyncService
{
    /// <summary>
    /// Connects to verified domestic trading nodes to query the live spot evaluation index 
    /// parameters for precious metal assets tracked in traditional Tolas.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>A strongly typed payload container holding localized bullion pricing benchmarks.</returns>
    Task<PreciousMetalValuationSnapshot> GetLiveCommodityRatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Isolates a single spot rate value per Tola matching your asset parameters.
    /// Natively called by TriggerApiSyncCommand and GetLiveChartsDataQuery to automate your silver/gold valuation updates.
    /// </summary>
    /// <param name="commodityName">The designation identifier of the bullion target (e.g., "Gold24K", "Silver").</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The real-time market rate per traditional Tola in PKR.</returns>
    Task<decimal> FetchSpotRatePerTolaAsync(string commodityName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly typed application layer record capturing current market rates per traditional Tola [CL-203].
/// </summary>
public record PreciousMetalValuationSnapshot(
    decimal Gold24KPricePerTolaPkr,
    decimal Gold22KPricePerTolaPkr,
    decimal FineSilverPricePerTolaPkr,
    DateTime LastUpdatedUtc
);