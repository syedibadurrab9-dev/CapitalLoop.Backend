using System;
using MediatR;

namespace Application.Chat.Commands.AcknowledgeChatMessage;

/// <summary>
/// CQRS Command used by back-office roles to explicitly claim an unaddressed chat thread node [CL-404].
/// </summary>
public record AcknowledgeChatMessageCommand(Guid MessageId, string OperatorEmail) : IRequest<bool>;