using MediatR;

namespace Application.AiAdvisor.Queries.GetRiskAuditSummary;

/// <summary>
/// CQRS Query to retrieve a comprehensive analytical summary of a user's portfolio risk matrix and exposure metrics [CL-302].
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for risk audit profiling.</param>
public record GetRiskAuditSummaryQuery(string UserEmail) : IRequest<RiskAuditSummaryDto>;