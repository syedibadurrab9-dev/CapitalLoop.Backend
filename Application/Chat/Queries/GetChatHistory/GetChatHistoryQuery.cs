using MediatR;
using System.Collections.Generic;

namespace Application.Portfolios.Queries;

/// <summary>
/// CQRS Query to retrieve highly optimized, historical conversational threads between two identity nodes [CL-401].
/// Enforces structural multi-tenant privacy boundaries at the core layer.
/// </summary>
/// <param name="CurrentEmail">The authenticated email address of the user initiating the history request.</param>
/// <param name="InteractingWithEmail">The destination target account email addressing the opposite conversational channel.</param>
public record GetChatHistoryQuery(string CurrentEmail, string InteractingWithEmail) : IRequest<IReadOnlyCollection<ChatHistoryDto>>;