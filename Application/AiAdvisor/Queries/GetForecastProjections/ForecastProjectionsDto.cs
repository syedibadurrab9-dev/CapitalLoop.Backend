using System.Collections.Generic;

namespace Application.Portfolios.Queries;

/// <summary>
/// Immutable wire DTO transferring calculated future growth tracks down to timeline charting widgets.
/// </summary>
public record ForecastProjectionsDto(
    decimal BaselineNetWorthPkr,
    decimal SimulatedAnnualGrowthRate,
    IReadOnlyList<string> TimelineLabels,
    IReadOnlyList<decimal> ProjectedValuesPkr,
    string AdvisoryNarrative,
    bool IsModelClippedBySubscriptionGate
);