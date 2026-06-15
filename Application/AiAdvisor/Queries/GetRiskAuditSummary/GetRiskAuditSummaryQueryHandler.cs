using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.AiAdvisor.Queries.GetRiskAuditSummary;




/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Execution Handler processing portfolio variance checks and compiling aggregate risk indicators 
/// out of realigned central context entities [CL-302].
/// Fully synchronized with the updated entity layer tracking changes.
/// </summary>
public class GetRiskAuditSummaryQueryHandler : IRequestHandler<GetRiskAuditSummaryQuery, RiskAuditSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetRiskAuditSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RiskAuditSummaryDto> Handle(GetRiskAuditSummaryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 HIGH-SPEED LOOKUP: Extract user profile parameters via an optimized, untracked stream
        var profile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.Email == sanitizedEmail)
            .Select(p => new { p.RiskMatrix, p.SuitabilityAssessmentScore })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"Risk audit compilation aborted: Profile context could not be resolved for identity '{sanitizedEmail}'.");
        }

        // 🎯 PERSISTENCE REALIGNMENT: Pull active portfolio assets from your realigned context table property 'AssetNodes'
        var userAssets = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new { a.Category, a.ValuationPkr }) // 🛠️ FIXED: Realigned to use your true domain model property bounds
            .ToListAsync(cancellationToken);

        decimal totalNetWorth = userAssets.Sum(a => a.ValuationPkr);

        // Compute high-volatility exposure balances (Crypto assets represent elevated volatility bounds)
        decimal highVolatilityValue = userAssets
            .Where(a => a.Category == AssetCategory.Crypto)
            .Sum(a => a.ValuationPkr);

        decimal cryptoExposurePercentage = totalNetWorth > 0
            ? (highVolatilityValue / totalNetWorth) * 100
            : 0;

        // 🧠 COMPLIANCE AUDIT ENGINE: Evaluate structural alignment against suitability thresholds
        string complianceStatus = "Compliant";
        string riskMitigationAdvice = "Your active portfolio concentration matches your verified risk tolerance thresholds perfectly.";

        // If a low-risk user holds an aggressive amount of highly volatile digital assets, flag a variance mismatch
        if (profile.SuitabilityAssessmentScore < 30 && cryptoExposurePercentage > 15)
        {
            complianceStatus = "Variance_Mismatch";
            riskMitigationAdvice = "Risk Alert: Your verified suitability profile indicates a conservative objective, but your digital asset concentration exceeds recommended thresholds (15%). Consider rebalancing into physical precious metals or stable mutual funds.";
        }
        else if (cryptoExposurePercentage > 50)
        {
            complianceStatus = "High_Exposure_Warning";
            riskMitigationAdvice = "Concentration Warning: Over 50% of your net worth is tied up in volatile asset nodes. Consider diversifying into defensive sectors to secure long-term capital loop milestones.";
        }

        return new RiskAuditSummaryDto(
            AssignedRiskTier: profile.RiskMatrix.ToString(),
            SuitabilityScore: profile.SuitabilityAssessmentScore,
            TotalTrackedValuePkr: totalNetWorth,
            HighVolatilityExposurePercentage: Math.Round(cryptoExposurePercentage, 2),
            AuditComplianceStatus: complianceStatus,
            MitigationNarrative: riskMitigationAdvice,
            AuditedAtUtc: DateTime.UtcNow
        );
    }
}