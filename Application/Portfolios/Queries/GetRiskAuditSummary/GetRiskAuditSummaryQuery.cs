using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetRiskAuditSummary;

// Immutable CQRS Request payload contract
public record GetRiskAuditSummaryQuery(string UserEmail, decimal MaxSectorCapPercentage) : IQuery<RiskAuditDto>;

// Structured response payload for React Native screens and Angular panels
public record RiskAuditDto(bool SafetyBreached, List<string> OverConcentratedSectors, string AdvisoryRemarks);

public class GetRiskAuditSummaryQueryHandler : IQueryHandler<GetRiskAuditSummaryQuery, RiskAuditDto>
{
    private readonly IApplicationDbContext _context;

    public GetRiskAuditSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RiskAuditDto> HandleAsync(GetRiskAuditSummaryQuery query, CancellationToken cancellationToken)
    {
        var usersAssets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == query.UserEmail.ToLowerInvariant())
            .ToListAsync(cancellationToken);

        var totalPortfolioValue = usersAssets.Sum(a => a.HoldingValuePKR);
        if (totalPortfolioValue == 0)
        {
            return new RiskAuditDto(false, new List<string>(), "No asset allocation mappings discovered.");
        }

        var breaches = new List<string>();

        var groupedByCategory = usersAssets.GroupBy(a => a.Category);

        foreach (var group in groupedByCategory)
        {
            var categoryTotal = group.Sum(a => a.HoldingValuePKR);

            // Calculate absolute sector balance weight percentages against overall net worth
            var actualWeight = (categoryTotal / totalPortfolioValue) * 100m;

            if (actualWeight > query.MaxSectorCapPercentage)
            {
                // Renders clean output strings like: "Crypto (42.5%)" or "Real_Estate (65.0%)"
                breaches.Add($"{group.Key.ToString()} ({actualWeight:F1}%)");
            }
        }

        // Generate tailored alert remarks to advise the user on portfolio rebalancing
        var remarks = breaches.Any()
            ? $"[VOLATILITY CRITICAL RISK DETECTED] Assets inside {string.Join(", ", breaches)} breach your customized guardrails. Rebalance capital immediately into mutual funds or silver."
            : "Portfolio architecture meets all compliance framework parameters smoothly.";

        return new RiskAuditDto(breaches.Any(), breaches, remarks);
    }
}