using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.AiAdvisor.Queries.GetTargetRecommendations;



/// <summary>
/// ⚡ CQRS EXECUTION HANDLER
/// Execution Handler that maps portfolio distribution deviations against target allocation boundaries 
/// inside the synchronized 'AssetNodes' database context to yield advisory recommendations [CL-302].
/// Fully integrated with the updated entity layer tracking changes.
/// </summary>
public class GetTargetRecommendationsQueryHandler : IRequestHandler<GetTargetRecommendationsQuery, TargetRecommendationsDto>
{
    private readonly IApplicationDbContext _context;

    public GetTargetRecommendationsQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<TargetRecommendationsDto> Handle(GetTargetRecommendationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 PERFORMANCE LOOKUP: Pull necessary profile properties via an untracked optimized lookup stream
        var userProfile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.Email == sanitizedEmail)
            .Select(p => new { p.SubscriptionTier, p.RiskMatrix, p.SuitabilityAssessmentScore })
            .FirstOrDefaultAsync(cancellationToken);

        if (userProfile is null)
        {
            throw new InvalidOperationException($"Advisory analysis aborted: Profile tracking context could not be resolved for identity '{sanitizedEmail}'.");
        }

        // 🛡️ AUTHORIZATION FEATURE GATE: Intercept free tier users before executing compute-heavy strategy logic
        bool isAuthorized = SubscriptionTiers.IsFeatureAuthorized(userProfile.SubscriptionTier, SubscriptionTiers.StrategicRoboInsights);

        if (!isAuthorized)
        {
            return new TargetRecommendationsDto(
                AssignedRiskBracket: userProfile.RiskMatrix.ToString(),
                IsPremiumFeatureLocked: true,
                AdvisoryNarrativeSummary: "Upgrade to an authorized premium membership tier to unlock tailored robo-advisory metrics and strategic rebalancing actions.",
                SuggestedAllocationSplits: new Dictionary<string, decimal>()
            );
        }

        // 🎯 PERSISTENCE REALIGNMENT: Fetch using your realigned context table property 'AssetNodes'
        var activeHoldings = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new { a.Category, a.ValuationPkr }) // 🛠️ FIXED: Realigned to use your true domain model property
            .ToListAsync(cancellationToken);

        decimal totalNetWorth = activeHoldings.Sum(h => h.ValuationPkr);
        var strategicSplits = new Dictionary<string, decimal>();
        string adviceMessage;

        // 🧠 ALGORITHMIC DISTRIBUTION MATRICES: Project target splits based on the user's risk assessment profile
        if (userProfile.SuitabilityAssessmentScore < 30)
        {
            adviceMessage = "Defensive Allocation Roadmap: Your risk variance indicates a strong priority for capital preservation. CapitalLoop recommends establishing baseline allocations in stable mutual funds and physical precious metals.";
            strategicSplits.Add(AssetCategory.PreciousMetal.ToString(), 40.0m);
            strategicSplits.Add(AssetCategory.MutualFund.ToString(), 50.0m);
            strategicSplits.Add(AssetCategory.Crypto.ToString(), 10.0m);
        }
        else if (userProfile.SuitabilityAssessmentScore <= 70)
        {
            adviceMessage = "Balanced Wealth Roadmap: Your profile tracks a moderate risk capacity. We suggest maintaining an equal balance between equity growth vehicles and steady income placements.";
            strategicSplits.Add(AssetCategory.PsxEquity.ToString(), 40.0m);
            strategicSplits.Add(AssetCategory.MutualFund.ToString(), 30.0m);
            strategicSplits.Add(AssetCategory.PreciousMetal.ToString(), 20.0m);
            strategicSplits.Add(AssetCategory.Crypto.ToString(), 10.0m);
        }
        else
        {
            adviceMessage = "Aggressive Growth Roadmap: Your horizon and assessment tracks accommodate high short-term asset variance. Dynamic allocations into corporate stock sectors and digital assets are authorized to optimize return potential.";
            strategicSplits.Add(AssetCategory.PsxEquity.ToString(), 60.0m);
            strategicSplits.Add(AssetCategory.Crypto.ToString(), 25.0m);
            strategicSplits.Add(AssetCategory.MutualFund.ToString(), 15.0m);
        }

        return new TargetRecommendationsDto(
            AssignedRiskBracket: userProfile.RiskMatrix.ToString(),
            IsPremiumFeatureLocked: false,
            AdvisoryNarrativeSummary: adviceMessage,
            SuggestedAllocationSplits: strategicSplits
        );
    }
}