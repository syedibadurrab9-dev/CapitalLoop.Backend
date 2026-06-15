using System.Collections.Generic;

namespace Application.AiAdvisor.Queries.GetTargetRecommendations;

/// <summary>
/// Immutable wire DTO transferring generated target portfolio distribution advice down to client dashboard widgets.
/// </summary>
public record TargetRecommendationsDto(
    string AssignedRiskBracket,
    bool IsPremiumFeatureLocked,
    string AdvisoryNarrativeSummary,
    IReadOnlyDictionary<string, decimal> SuggestedAllocationSplits
);