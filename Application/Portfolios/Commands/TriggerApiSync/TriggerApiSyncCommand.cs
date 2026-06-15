using MediatR;

namespace Application.Portfolios.Commands.TriggerApiSync;

/// <summary>
/// CQRS Command to force an immediate, live server-side synchronization of an automated asset node [CL-201].
/// Re-evaluates balance metrics against current real-time market foreign exchange and spot values.
/// </summary>
/// <param name="UserEmail">The authenticated user email address asserting ownership over the target node.</param>
/// <param name="AssetNodeId">The unique aggregate primary tracking key of the target asset position.</param>
public record TriggerApiSyncCommand(string UserEmail, Guid AssetNodeId) : IRequest<bool>;