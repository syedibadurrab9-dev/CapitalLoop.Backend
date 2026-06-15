using System;

namespace Application.AiAdvisor.Queries.GetRoboAdvisorySnapshot;

/// <summary>
/// Strongly-typed immutable wire contract for transmitting generated robo insights down to client layers.
/// </summary>
public record RoboAdvisorySnapshotDto(
    string CurrentRiskTolerance,
    int AssessmentScore,
    int TotalAssetsTrackedCount,
    decimal AccumulatedNetWorthPkr,
    bool IsTaxFilerCompiled,
    string GeneratedInsightsText,
    DateTime CalculatedAtUtc
);