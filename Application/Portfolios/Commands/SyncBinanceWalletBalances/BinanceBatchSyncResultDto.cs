namespace Application.Portfolios.Commands.SyncBinanceWalletBalances;

/// <summary>
/// Immutable telemetry record detailing batch synchronization summary statistics for tracking worker execution states.
/// </summary>
public record BinanceBatchSyncResultDto(
    int ProcessedCount,
    int SuccessfulCount,
    int FailedCount
);