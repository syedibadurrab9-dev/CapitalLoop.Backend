using System.Collections.Generic;

namespace Application.AiAdvisor.Queries.GetAssetAllocationDeficiencies;

/// <summary>
/// Strongly-typed immutable wire contract capturing detected asset imbalance records.
/// </summary>
public record AllocationAnalysisDto(
    decimal TotalNetWorthPkr,
    IReadOnlyCollection<string> DetectedDiscrepancies,
    bool IsRebalancingActionRequired
);