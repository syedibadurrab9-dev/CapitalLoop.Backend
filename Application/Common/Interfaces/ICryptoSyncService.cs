using Application.Common.DTOs;

namespace Application.Common.Interfaces;

public interface ICryptoSyncService
{
    /// <summary>
    /// Executes a concurrent master sweep across a user's linked exchange profile.
    /// Aggregates and normalizes overall Spot, Earn, and Futures balances into localized PKR.
    /// </summary>
    Task<BinanceSyncResult> FetchExtendedPortfolioBalancesAsync(string apiKey, string secretKey);

    /// <summary>
    /// Targets isolated spot wallet balances. 
    /// Essential for lightweight, high-frequency dashboard summary updates.
    /// </summary>
    Task<decimal> GetSpotWalletBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls liquidation metrics, margin maintenance minimums, and active risk valuations.
    /// Crucial for isolating high-leverage positions before computing strategic age-gap safety scores.
    /// </summary>
    Task<decimal> GetFuturesWalletBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts data from flexible, locked, and automated high-yield staking pools.
    /// Maps to long-term wealth building metrics within the portfolio auditing module.
    /// </summary>
    Task<decimal> GetSimpleEarnBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default);
}