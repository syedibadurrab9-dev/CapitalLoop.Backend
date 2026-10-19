using Application.Common.Interfaces;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetLiveChartsData;

public record GetLiveChartsDataQuery(string UserEmail) : IQuery<List<ChartDataPointDto>>;

public record ChartDataPointDto(string AssetLabel, string CategoryType, decimal EvaluationValue, bool AutoSynced);

public class GetLiveChartsDataQueryHandler : IQueryHandler<GetLiveChartsDataQuery, List<ChartDataPointDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLiveChartsDataQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChartDataPointDto>> HandleAsync(GetLiveChartsDataQuery query, CancellationToken cancellationToken)
    {
        var activeAssets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == query.UserEmail.ToLowerInvariant())
            .OrderByDescending(a => a.HoldingValuePKR)
            .Select(a => new ChartDataPointDto(
                a.Name,
                a.Category.ToString(),
                a.HoldingValuePKR,
                a.IsAutoSynced
            ))
            .ToListAsync(cancellationToken);

        return activeAssets;
    }
}
