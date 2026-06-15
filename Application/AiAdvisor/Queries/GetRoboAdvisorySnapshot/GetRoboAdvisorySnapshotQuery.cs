using Application.Common.DTOs;
using MediatR;

namespace Application.AiAdvisor.Queries.GetRoboAdvisorySnapshot;

/// <summary>
/// CQRS Query contract for retrieving an aggregated wealth matrix and robo-advisory snapshot [CL-302].
/// Enforces strict security validation context boundaries at the pipeline validation level.
/// </summary>
/// <param name="UserEmail">The authenticated, unique email address tracking context of the executing user profile.</param>
public record GetRoboAdvisorySnapshotQuery(string UserEmail) : IRequest<RoboAdvisorySnapshotDto>;