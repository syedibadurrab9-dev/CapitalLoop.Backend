using Infrastructure.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Portfolios.Queries.GetAggregatedNetWorth;
public record GetAggregatedNetWorthQuery(string UserEmail, string TargetedCurrency) : IRequest<NetWorthDto>;

public record NetWorthDto(decimal TotalCalculatedValue, string CurrencyToken);

public class GetAggregatedNetWorthQueryHandler : IRequestHandler<GetAggregatedNetWorthQuery, NetWorthDto>
{
    private readonly IApplicationDbContext _context;
    private const decimal MockExchangeRate = 278.50m; // Central PKR/USD transformation variable

    public GetAggregatedNetWorthQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NetWorthDto> Handle(GetAggregatedNetWorthQuery request, CancellationToken cancellationToken)
    {
        var totalPKR = await _context.Assets
            .Where(a => a.UserEmail == request.UserEmail.ToLowerInvariant())
            .SumAsync(a => a.HoldingValuePKR, cancellationToken);

        if (request.TargetedCurrency.ToUpperInvariant() == "USD")
        {
            return new NetWorthDto(Math.Round(totalPKR / MockExchangeRate, 2), "USD");
        }

        return new NetWorthDto(totalPKR, "PKR");
    }
}