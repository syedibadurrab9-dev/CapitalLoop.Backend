using System.Collections.Generic;
using Application.Common.DTOs;
using MediatR;

namespace Application.Portfolios.Queries.GetAssetNodesList;

/// <summary>
/// CQRS Query to compile a listed breakdown of individual asset nodes featuring calculated percentage splits [CL-203].
/// </summary>
/// <param name="UserEmail">The authenticated user email address targeted for breakdown extraction.</param>
public record GetAssetNodesListQuery(string UserEmail) : IRequest<IReadOnlyCollection<AssetNodeTelemetryDto>>;