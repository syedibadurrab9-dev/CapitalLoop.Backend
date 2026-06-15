using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Chat.Commands.MarkMessagesAsRead;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Runs automated batch updates over unread messaging arrays matching target channel indices,
/// verifying boundary constraints and appending operator audit traces cleanly [CL-101].
/// </summary>
public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, int>
{
    private readonly IApplicationDbContext _context;

    public MarkMessagesAsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> Handle(MarkMessagesAsReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetUser = command.TargetUserEmail.Trim().ToLowerInvariant();
        var interactingWith = command.InteractingWithEmail.Trim().ToLowerInvariant();

        // Query unread incoming messages streaming from the specific target channel context
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ReceiverEmail == targetUser && m.SenderEmail == interactingWith && !m.IsRead)
            .ToListAsync(cancellationToken);

        if (!unreadMessages.Any())
        {
            return 0;
        }

        // =============================================================================
        // 🧱 RESOLVES CS7036: PASS COMPLIANCE TELEMETRY FOOTPRINT TO MUTATOR
        // =============================================================================
        // Mutate state structures through the encapsulated domain aggregate methods safely.
        // Appended 'targetUser' as the required tracking string parameter to trace mutations back cleanly.
        foreach (var message in unreadMessages)
        {
            message.MarkAsRead(targetUser);
        }

        // Commits the modified entity states in a single atomic transaction block, returning rows affected count
        return await _context.SaveChangesAsync(cancellationToken);
    }
}