using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetRealEstateTelemetry;

public record RealEstatePropertyTelemetryDto(
    Guid AssetId,
    string Name,
    int PaidInstallments,
    int TotalInstallments,
    double ProgressPercentage,
    decimal PaidEquityPKR,
    decimal EstimatedTotalValuePKR,
    decimal OutstandingCommitmentPKR,
    DateTime LastUpdatedUtc
);

public record RealEstateCashflowSummaryDto(
    decimal TotalPaidEquityPKR,
    decimal TotalOutstandingCommitmentPKR,
    decimal TotalEstimatedPortfolioValuePKR,
    double OverallProgressPercentage,
    List<RealEstatePropertyTelemetryDto> Properties
);

public record GetRealEstateTelemetryQuery(string UserEmail) : IQuery<RealEstateCashflowSummaryDto>;

public class GetRealEstateTelemetryQueryHandler : IQueryHandler<GetRealEstateTelemetryQuery, RealEstateCashflowSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetRealEstateTelemetryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RealEstateCashflowSummaryDto> HandleAsync(GetRealEstateTelemetryQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        var reAssets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == normalizedEmail && a.Category == AssetCategory.Real_Estate)
            .ToListAsync(cancellationToken);

        var propertyList = new List<RealEstatePropertyTelemetryDto>(reAssets.Count);
        decimal totalPaidEquity = 0;
        decimal totalOutstandingCommitment = 0;
        decimal totalEstimatedValue = 0;

        foreach (var asset in reAssets)
        {
            var paid = asset.InstallmentsPaid ?? 0;
            var total = asset.TotalInstallments ?? 1;

            var progress = total > 0 ? Math.Round((double)paid / total * 100, 2) : 0.0;

            decimal estimatedTotalValue;
            if (paid > 0 && total > 0)
            {
                var fraction = (decimal)paid / total;
                estimatedTotalValue = Math.Round(asset.HoldingValuePKR / fraction, 2);
            }
            else
            {
                estimatedTotalValue = asset.HoldingValuePKR;
            }

            var outstanding = Math.Max(0m, estimatedTotalValue - asset.HoldingValuePKR);

            totalPaidEquity += asset.HoldingValuePKR;
            totalOutstandingCommitment += outstanding;
            totalEstimatedValue += estimatedTotalValue;

            propertyList.Add(new RealEstatePropertyTelemetryDto(
                AssetId: asset.Id,
                Name: asset.Name,
                PaidInstallments: paid,
                TotalInstallments: total,
                ProgressPercentage: progress,
                PaidEquityPKR: asset.HoldingValuePKR,
                EstimatedTotalValuePKR: estimatedTotalValue,
                OutstandingCommitmentPKR: outstanding,
                LastUpdatedUtc: asset.LastUpdatedUtc
            ));
        }

        var overallProgress = totalEstimatedValue > 0
            ? Math.Round((double)(totalPaidEquity / totalEstimatedValue) * 100, 2)
            : 0.0;

        return new RealEstateCashflowSummaryDto(
            TotalPaidEquityPKR: totalPaidEquity,
            TotalOutstandingCommitmentPKR: totalOutstandingCommitment,
            TotalEstimatedPortfolioValuePKR: totalEstimatedValue,
            OverallProgressPercentage: overallProgress,
            Properties: propertyList
        );
    }
}
