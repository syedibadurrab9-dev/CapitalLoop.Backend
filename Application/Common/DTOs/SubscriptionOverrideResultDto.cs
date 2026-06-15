using System;

namespace Application.Common.DTOs;

/// <summary>
/// 💳 SUBSCRIPTION OVERRIDE RESPONSE CONTRACT [CL-503]
/// Unified response payload capturing the execution status of a subscription change operation.
/// </summary>
public record SubscriptionOverrideResultDto
{
    public bool IsOverrideExecutionSuccessful { get; init; }
    public string UserEmailTarget { get; init; } = string.Empty;
    public string AssignedActiveTier { get; init; } = string.Empty;
    public string UpdatedSecurityJwtToken { get; init; } = string.Empty;
    public DateTime ProcessedTimestampUtc { get; init; } = DateTime.UtcNow;
}