using Application.Common.Interfaces;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetAggregatedNetWorth;

public record GetAggregatedNetWorthQuery(string UserEmail, string TargetedCurrency) : IQuery<NetWorthDto>;

public record NetWorthDto(decimal TotalCalculatedValue, string CurrencyToken);

public class GetAggregatedNetWorthQueryHandler : IQueryHandler<GetAggregatedNetWorthQuery, NetWorthDto>
{
    private readonly IApplicationDbContext _context;
    private const decimal MockExchangeRate = 278.50m; // Central PKR/USD transformation variable

    public GetAggregatedNetWorthQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NetWorthDto> HandleAsync(GetAggregatedNetWorthQuery query, CancellationToken cancellationToken)
    {
        var totalPKR = await _context.Assets
            .Where(a => a.UserEmail == query.UserEmail.ToLowerInvariant())
            .SumAsync(a => a.HoldingValuePKR, cancellationToken);

        if (query.TargetedCurrency.ToUpperInvariant() == "USD")
        {
            return new NetWorthDto(Math.Round(totalPKR / MockExchangeRate, 2), "USD");
        }

        return new NetWorthDto(totalPKR, "PKR");
    }
}