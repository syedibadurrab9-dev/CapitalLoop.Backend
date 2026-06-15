using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Chat.Commands.AcknowledgeChatMessage;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Handles chat message read acknowledgments, verifying operational security boundaries 
/// and updating data persistence tables with strict enterprise auditing telemetry [CL-101].
/// </summary>
public class AcknowledgeChatMessageCommandHandler : IRequestHandler<AcknowledgeChatMessageCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public AcknowledgeChatMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> Handle(AcknowledgeChatMessageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.OperatorEmail))
        {
            throw new ArgumentException("Acknowledge operation rejected: Operator email tracking coordinate cannot be null or empty.", nameof(command.OperatorEmail));
        }

        var sanitizedOperatorEmail = command.OperatorEmail.Trim().ToLowerInvariant();

        // 🎯 PERSISTENCE ALIGNMENT: Resolves records cleanly out of your tracking context properties
        var message = await _context.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == command.MessageId, cancellationToken);

        if (message is null)
        {
            throw new InvalidOperationException($"Chat mutation error: Targeted message node reference '{command.MessageId}' does not exist.");
        }

        // 🛡️ SECURITY VERIFICATION: Prevent operators from acknowledging their own outgoing advisor statements
        if (message.SenderEmail.Equals(sanitizedOperatorEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // =============================================================================
        // 🧱 RESOLVES CS7036: PASS COMPLIANCE TELEMETRY FOOTPRINT TO MUTATOR
        // =============================================================================
        // Enforce encapsulated domain transition rules safely by passing the active tracking signature
        message.Acknowledge(sanitizedOperatorEmail);

        // 🎯 TRANSACTIONAL PERSISTENCE: Save state updates cleanly to SQL Server storage layers
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}