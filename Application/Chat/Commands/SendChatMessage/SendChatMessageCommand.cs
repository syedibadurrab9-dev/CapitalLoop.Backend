using System;
using MediatR;

namespace Application.Chat.Commands.SendChatMessage;

/// <summary>
/// CQRS Command to transmit and persist an interactive real-time chat message transaction [CL-401].
/// Supports optional Base64 decentralized chart deep-link attachments for telemetry tracking [CL-403].
/// </summary>
public record SendChatMessageCommand(
    string SenderEmail,
    string ReceiverEmail,
    string MessageText,
    string? ChartTokenPayload = null) : IRequest<Guid>;