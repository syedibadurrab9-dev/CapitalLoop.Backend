using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Commands.LinkBinanceWallet;

public record LinkBinanceWalletCommand : IRequest<BinanceSyncResult>
{
    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
}