using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Chat.Commands.GetUnreadMessagesCount;



/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Allocation-Insulated Handler yielding aggregate summary statistics for incoming message states [CL-701].
/// Fully synchronized with the 'ChatMessages' database context mapping definition.
/// </summary>
public class GetUnreadMessagesCountQueryHandler : IRequestHandler<GetUnreadMessagesCountQuery, int>
{
    private readonly IApplicationDbContext _context;

    public GetUnreadMessagesCountQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> Handle(GetUnreadMessagesCountQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 SCALABILITY FIX: Run a highly performant CountAsync sweep directly inside SQL Server.
        // Prevents loading large historical records into system RAM allocations just to compute integers.
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ReceiverEmail == sanitizedEmail && !m.IsRead)
            .CountAsync(cancellationToken);
    }
}