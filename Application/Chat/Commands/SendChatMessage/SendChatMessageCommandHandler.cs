using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Chat.Commands.SendChatMessage;


public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SendChatMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Guid> Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 🎯 DOMAIN ENCAPSULATION: Pass arguments directly. 
        // The ChatMessage.Create domain factory handles validation and sanitization internally.
        var chatMessage = ChatMessage.Create(
            senderEmail: command.SenderEmail,
            receiverEmail: command.ReceiverEmail,
            text: command.MessageText,
            chartToken: command.ChartTokenPayload
        );

        // Stage the fresh aggregate record across your synchronized database context collection
        await _context.ChatMessages.AddAsync(chatMessage, cancellationToken);

        // Atomically commit state alterations down to relational database partitions
        await _context.SaveChangesAsync(cancellationToken);

        // 🚀 Return the persistent base Entity tracking ID to facilitate downstream real-time SignalR hub dispatch loops
        return chatMessage.Id;
    }
}