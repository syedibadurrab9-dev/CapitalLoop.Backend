using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Queries;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-Performance Execution Handler processing two-way relational conversational message records [CL-702].
/// Leverages fully un-tracked projections to preserve execution memory footprints and enforce isolation bounds.
/// </summary>
public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, IReadOnlyCollection<ChatHistoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetChatHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyCollection<ChatHistoryDto>> Handle(GetChatHistoryQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var userA = query.CurrentEmail.Trim().ToLowerInvariant();
        var userB = query.InteractingWithEmail.Trim().ToLowerInvariant();

        // 🚀 PERFORMANCE & SECURITY FIX: Query a bidirectional, non-tracked database projection.
        // 🧱 FIX: Realigned sorting mechanism to use the historical 'CreatedAt' inherited timestamp 
        // to prevent sequence scrambling caused by non-sequential Guid primary key properties.
        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => (m.SenderEmail == userA && m.ReceiverEmail == userB) ||
                        (m.SenderEmail == userB && m.ReceiverEmail == userA))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatHistoryDto(
                m.Id,
                m.SenderEmail,
                m.ReceiverEmail,
                m.MessageText,
                m.ChartTokenPayload,
                m.IsAcknowledgedByOperator,
                m.IsRead
            ))
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }
}