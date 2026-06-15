using MediatR;

namespace Application.Portfolios.Commands.SyncBinanceWalletBalances;

/// <summary>
/// CQRS Background Automation Command to execute a batch synchronization sweep across active Binance wallets [CL-201].
/// Typically triggered by system background workers or high-frequency automated polling infrastructure.
/// </summary>
/// <param name="BatchSize">The maximum number of wallet records to process within this single transaction execution loop.</param>
public record SyncBinanceWalletBalancesCommand(int BatchSize = 50) : IRequest<BinanceBatchSyncResultDto>;