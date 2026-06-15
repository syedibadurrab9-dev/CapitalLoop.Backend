using MediatR;

namespace Application.Chat.Commands.GetUnreadMessagesCount;

/// <summary>
/// CQRS Query counting pending, unopened communication tokens across incoming interaction lines [CL-501].
/// Used to instantly drive high-frequency badge counters down on client workspace shells.
/// </summary>
/// <param name="UserEmail">The authenticated email context targeting the recipient profile.</param>
public record GetUnreadMessagesCountQuery(string UserEmail) : IRequest<int>;