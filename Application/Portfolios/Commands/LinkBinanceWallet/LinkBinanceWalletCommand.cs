using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Commands.LinkBinanceWallet;

/// <summary>
/// CQRS Command used to link or refresh an external Binance exchange wallet integration profile [CL-201].
/// Extracted into an immutable data wire record to preserve execution context stability.
/// </summary>
public record LinkBinanceWalletCommand : IRequest<BinanceSyncResultDto>
{
    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
}