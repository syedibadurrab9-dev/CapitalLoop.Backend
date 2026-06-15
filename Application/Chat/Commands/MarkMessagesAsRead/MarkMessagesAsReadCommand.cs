using MediatR;

namespace Application.Chat.Commands.MarkMessagesAsRead;

/// <summary>
/// CQRS Command to bulk-synchronize visibility state maps once a conversation loop cell enters view [CL-501].
/// </summary>
public record MarkMessagesAsReadCommand(string TargetUserEmail, string InteractingWithEmail) : IRequest<int>;