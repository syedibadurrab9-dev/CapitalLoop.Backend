using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Queries;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-Performance Query Handler processing total net-worth aggregates and tracking summary KPIs [CL-201].
/// Leverages high-speed single-pass SQL aggregation to protect memory pools from heap allocation bursts.
/// </summary>
public class GetPortfolioSummaryQueryHandler : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetPortfolioSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PortfolioSummaryDto> Handle(GetPortfolioSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 EFFICIENCY OPTIMIZATION: Process calculations directly inside SQL Server rather than pulling rows to memory
        var aggregationSummary = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .GroupBy(a => 1) // Force a unified single-record scalar aggregation loop block
            .Select(g => new
            {
                TotalValuePkr = g.Sum(a => a.ValuationPkr),
                TotalCount = g.Count(),
                AutoCount = g.Count(a => a.IsAutoSynced)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (aggregationSummary is null || aggregationSummary.TotalCount == 0)
        {
            return new PortfolioSummaryDto(
                TotalPortfolioValuePkr: 0.00m,
                TotalNodesCount: 0,
                AutomatedSyncNodesCount: 0,
                ManualNodesCount: 0,
                LastUpdatedUtc: DateTime.UtcNow
            );
        }

        int manualNodesCount = aggregationSummary.TotalCount - aggregationSummary.AutoCount;

        return new PortfolioSummaryDto(
            TotalPortfolioValuePkr: Math.Round(aggregationSummary.TotalValuePkr, 2),
            TotalNodesCount: aggregationSummary.TotalCount,
            AutomatedSyncNodesCount: aggregationSummary.AutoCount,
            ManualNodesCount: manualNodesCount,
            LastUpdatedUtc: DateTime.UtcNow
        );
    }
}