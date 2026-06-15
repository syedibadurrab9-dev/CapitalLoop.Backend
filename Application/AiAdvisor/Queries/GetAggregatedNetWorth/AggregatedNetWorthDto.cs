using System;
using System.Collections.Generic;

namespace Application.AiAdvisor.Queries.GetAggregatedNetWorth;

/// <summary>
/// Immutable return transfer payload mapping net-worth sums and dynamic category weights to UI components.
/// </summary>
public record AggregatedNetWorthDto(
    decimal TotalNetWorthPkr,
    IReadOnlyDictionary<string, decimal> AssetClassBalances,
    IReadOnlyDictionary<string, decimal> AssetClassPercentages,
    DateTime CalculatedAtUtc
);