using System;

namespace Application.Portfolios.Queries;

/// <summary>
/// Immutable DTO record payload tracking core aggregate statistics for dashboard headers.
/// </summary>
public record PortfolioSummaryDto(
    decimal TotalPortfolioValuePkr,
    int TotalNodesCount,
    int AutomatedSyncNodesCount,
    int ManualNodesCount,
    DateTime LastUpdatedUtc
);