using Application.Common.Interfaces;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.AiAdvisor.Queries.GetRoboAdvisorySnapshot;



/// <summary>
/// ⚡ CQRS EXECUTION HANDLER
/// Pulls active user profile parameters, evaluates investment horizons, and provides unified operational snapshots [CL-303].
/// Fully synchronized with the updated entity layer tracking changes and pluralized database context layout.
/// </summary>
public class GetRoboAdvisorySnapshotQueryHandler : IRequestHandler<GetRoboAdvisorySnapshotQuery, RoboAdvisorySnapshotDto>
{
    private readonly IApplicationDbContext _context;

    public GetRoboAdvisorySnapshotQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<RoboAdvisorySnapshotDto> Handle(GetRoboAdvisorySnapshotQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 PERFORMANCE FIX: Execute a fully un-tracked, optimized lookup projection pipeline.
        // This isolates tenant profiles and protects the database engine from memory footprint leaks.
        var profileData = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.Email == sanitizedEmail)
            .Select(p => new
            {
                p.SubscriptionTier,
                p.RiskMatrix,
                p.SuitabilityAssessmentScore,
                p.IsFiler,
                AssetCount = p.AssetNodes.Count,
                TotalPortfolioValue = p.AssetNodes.Sum(a => a.ValuationPkr) // 🛠️ FIXED: Mapped to synchronized 'ValuationPkr' property bounds
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profileData is null)
        {
            throw new InvalidOperationException($"Robo-advisor execution failed: Target profile context record could not be resolved for identity '{sanitizedEmail}'.");
        }

        // 🛡️ SECURITY CONTROL GATE: Enforce feature gate rules directly inside the application layer
        bool isRoboInsightsAuthorized = SubscriptionTiers.IsFeatureAuthorized(profileData.SubscriptionTier, SubscriptionTiers.StrategicRoboInsights);

        var genericAdviceMessage = isRoboInsightsAuthorized
            ? $"Based on your {profileData.RiskMatrix} allocation threshold profile score ({profileData.SuitabilityAssessmentScore}), your structural positions are stable. Leverage automated exchange synchronization channels to preserve execution precision."
            : "Upgrade to an authorized premium membership tier to unlock tailored robo-advisory metrics, deep ledger insights, and strategic rebalancing suggestions.";

        return new RoboAdvisorySnapshotDto(
            CurrentRiskTolerance: profileData.RiskMatrix.ToString(),
            AssessmentScore: profileData.SuitabilityAssessmentScore,
            TotalAssetsTrackedCount: profileData.AssetCount,
            AccumulatedNetWorthPkr: Math.Round(profileData.TotalPortfolioValue, 2),
            IsTaxFilerCompiled: profileData.IsFiler,
            GeneratedInsightsText: genericAdviceMessage,
            CalculatedAtUtc: DateTime.UtcNow
        );
    }
}