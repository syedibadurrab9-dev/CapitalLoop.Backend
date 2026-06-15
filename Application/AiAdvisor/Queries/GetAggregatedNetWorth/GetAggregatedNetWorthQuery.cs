using MediatR;

namespace Application.AiAdvisor.Queries.GetAggregatedNetWorth;

/// <summary>
/// CQRS Query to compute a user's total consolidated net worth across all manual and synced asset classes [CL-302].
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for portfolio consolidation.</param>
public record GetAggregatedNetWorthQuery(string UserEmail) : IRequest<AggregatedNetWorthDto>;