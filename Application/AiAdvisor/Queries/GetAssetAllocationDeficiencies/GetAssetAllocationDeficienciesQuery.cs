using MediatR;

namespace Application.AiAdvisor.Queries.GetAssetAllocationDeficiencies;

/// <summary>
/// CQRS Query for evaluating a user's asset allocation variances against localized structural asset class limits [CL-203].
/// Used directly to drive dynamic advice layouts in front-end client dashboards.
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for portfolio composition diagnosis.</param>
public record GetAssetAllocationDeficienciesQuery(string UserEmail) : IRequest<AllocationAnalysisDto>;