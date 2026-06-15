using MediatR;

namespace Application.Portfolios.Queries;

/// <summary>
/// CQRS Query to retrieve a comprehensive, high-level portfolio valuation and asset metrics summary [CL-203].
/// Resolves raw asset holdings, metal allocations, and cash positions into high-level dashboard summaries.
/// </summary>
/// <param name="UserEmail">The authenticated email address tracking the user workspace to compile.</param>
public record GetPortfolioSummaryQuery(string UserEmail) : IRequest<PortfolioSummaryDto>;