using Application.Common.DTOs;
using Application.Common.Interfaces;

namespace Application.Portfolios.Commands.LinkBinanceWallet;

public record LinkBinanceWalletCommand : ICommand<BinanceSyncResult>
{
    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
}