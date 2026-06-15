using System;

namespace Application.Portfolios.Commands;

/// <summary>
/// Immutable wire DTO transferring the outcome of the risk metric calculations down to the client layout.
/// </summary>
public record SuitabilityResultDto(
    string AssignedRiskTier,
    int FinalCalculatedScore,
    DateTime AssessmentCompletedAtUtc
);