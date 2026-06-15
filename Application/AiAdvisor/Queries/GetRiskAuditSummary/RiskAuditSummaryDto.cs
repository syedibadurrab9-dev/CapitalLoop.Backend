using System;

namespace Application.AiAdvisor.Queries.GetRiskAuditSummary;

/// <summary>
/// Immutable wire data transfer record detailing portfolio volatility and risk compliance checks.
/// </summary>
public record RiskAuditSummaryDto(
    string AssignedRiskTier,
    int SuitabilityScore,
    decimal TotalTrackedValuePkr,
    decimal HighVolatilityExposurePercentage,
    string AuditComplianceStatus,
    string MitigationNarrative,
    DateTime AuditedAtUtc
);