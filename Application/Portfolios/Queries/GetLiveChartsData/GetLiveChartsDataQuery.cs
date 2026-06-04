using Infrastructure.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Portfolios.Queries.GetLiveChartsData;

public record GetLiveChartsDataQuery(string UserEmail) : IRequest<List<ChartDataPointDto>>;

public record ChartDataPointDto(string AssetLabel, string CategoryType, decimal EvaluationValue, bool AutoSynced);

public class GetLiveChartsDataQueryHandler : IRequestHandler<GetLiveChartsDataQuery, List<ChartDataPointDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLiveChartsDataQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChartDataPointDto>> Handle(GetLiveChartsDataQuery request, CancellationToken cancellationToken)
    {
        var activeAssets = await _context.Assets
            .Where(a => a.UserEmail == request.UserEmail.ToLowerInvariant())
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
