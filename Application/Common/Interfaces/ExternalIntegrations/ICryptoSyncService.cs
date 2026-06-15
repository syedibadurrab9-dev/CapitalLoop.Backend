using System.Threading;
using System.Threading.Tasks;
using Application.Common.DTOs; // Maps perfectly to your centralized DTO assemblies

namespace Application.Common.Interfaces.ExternalIntegrations;

/// <summary>
/// Domain infrastructure contract designed to handle real-time balance aggregation 
/// and telemetry extraction across external connected crypto exchanges [CL-201].
/// </summary>
public interface ICryptoSyncService
{
    /// <summary>
    /// Validates third-party API keys with external infrastructure sync services before data persistence occurs.
    /// Used natively by LinkBinanceWalletCommand to catch validation faults early.
    /// </summary>
    /// <param name="apiKey">The encrypted or raw API key token sequence to test connect.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be canceled.</param>
    /// <returns>A validation result containing status parameters and verification metrics.</returns>
    Task<BinanceSyncResultDto> ValidateCredentialsAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates total localized net wallet valuations in PKR across all exchange buckets via a simple tracking token.
    /// Used directly by your automated batch worker loops running SyncBinanceWalletBalancesCommand.
    /// </summary>
    /// <param name="encryptedApiKey">The secure tracking key token stored under database rows.</param>
    /// <param name="cancellationToken">Propagates notifications that operations should be canceled.</param>
    /// <returns>The total evaluated wallet sum represented in PKR.</returns>
    Task<decimal> FetchLiveWalletBalanceAsync(string encryptedApiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a concurrent master sweep across a user's linked exchange profile.
    /// Aggregates and normalizes overall Spot, Earn, and Futures balances into localized PKR structures.
    /// </summary>
    /// <param name="credentials">The secure API identifier and signing key configurations.</param>
    /// <returns>A consolidated sync result containing portfolio metrics mapping to asset nodes.</returns>
    Task<BinanceSyncResultDto> FetchExtendedPortfolioBalancesAsync(CryptoCredentialsPayload credentials);

    /// <summary>
    /// Targets isolated spot wallet balances. 
    /// Essential for lightweight, high-frequency dashboard summary updates.
    /// </summary>
    Task<decimal> GetSpotWalletBalanceInUsdAsync(CryptoCredentialsPayload credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls liquidation metrics, margin maintenance minimums, and active risk valuations.
    /// Crucial for isolating high-leverage positions before computing strategic safety metrics.
    /// </summary>
    Task<decimal> GetFuturesWalletBalanceInUsdAsync(CryptoCredentialsPayload credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts data from flexible, locked, and automated high-yield staking pools.
    /// Maps to long-term wealth building metrics within the portfolio auditing module.
    /// </summary>
    Task<decimal> GetSimpleEarnBalanceInUsdAsync(CryptoCredentialsPayload credentials, CancellationToken cancellationToken = default);
}

/// <summary>
/// Secure context payload holding decrypted exchange API tokens extracted from database storage tracks [CL-201].
/// </summary>
public record CryptoCredentialsPayload(
    string EncryptedApiKey,
    string EncryptedSecretKey
);