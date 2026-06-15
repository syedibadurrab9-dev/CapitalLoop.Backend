using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.ExternalIntegrations;

/// <summary>
/// Domain infrastructure contract designed to map data points extracted from daily 
/// brokerage research and settled stock trade confirmations down to active ledger nodes [CL-414].
/// </summary>
public interface IAkdBrokerageSyncService
{
    /// <summary>
    /// Connects to the secure data ingestion stream, parses actual shares inventories 
    /// from settled AKD trade confirmations, and evaluates their real-time PKR values.
    /// </summary>
    /// <param name="email">The target identifier account associated with the trading record trail.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be canceled.</param>
    /// <returns>A read-only collection of parsed security balance parameters.</returns>
    Task<IReadOnlyCollection<AkdPositionPayload>> GetLiveSharesInventoryAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates total liquid ledger valuations across all active positions for high-speed net-worth evaluation queries.
    /// </summary>
    /// <param name="email">The target investor email account identifier.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be canceled.</param>
    /// <returns>The total cash and equity evaluation sum in PKR.</returns>
    Task<decimal> FetchBrokerageBalanceAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly typed context snapshot holding parsed securities data extracted from brokerage communication structures.
/// </summary>
public record AkdPositionPayload(
    string TickerSymbol,
    long ShareQuantity,
    decimal CurrentMarketPricePkr,
    decimal TotalPositionValuePkr,
    DateTime AsOfUtc
);