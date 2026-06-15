using System;
using System.Collections.Generic;

namespace Application.Common.DTOs;

/// <summary>
/// Represents an available subscription license tier within the CapitalLoop ecosystem.
/// Maps directly to the membership capabilities required for the Tier Manager and premium feature gates [CL-503].
/// </summary>
public record SubscriptionPlanDto
{
    public string PlanTierToken { get; init; } = string.Empty;
    public string FriendlyPlanMoniker { get; init; } = string.Empty;
    public decimal MonthlyBillingAmountPkr { get; init; }
    public IReadOnlyCollection<string> AuthorizedSystemFeatures { get; init; } = Array.Empty<string>();
}