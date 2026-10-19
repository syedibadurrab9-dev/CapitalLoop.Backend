using Application.Common.Interfaces;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Queries.GetAccountAuditHistory;

public record ComplianceAuditTrailLogDto(
    Guid LogId,
    string TargetEmail,
    string ReviewerAgentEmail,
    string ActionPerformed,
    string? Comments,
    DateTime Timestamp
);

public record GetAccountAuditHistoryQuery(string Email) : IQuery<List<ComplianceAuditTrailLogDto>>;

public class GetAccountAuditHistoryQueryHandler : IQueryHandler<GetAccountAuditHistoryQuery, List<ComplianceAuditTrailLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAccountAuditHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ComplianceAuditTrailLogDto>> HandleAsync(GetAccountAuditHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = query.Email.Trim().ToLowerInvariant();

        return await _context.ComplianceAuditLogs
            .AsNoTracking()
            .Where(l => l.TargetEmail == normalizedEmail)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new ComplianceAuditTrailLogDto(
                l.Id,
                l.TargetEmail,
                l.ReviewerAgentEmail,
                l.ActionPerformed,
                l.Comments,
                l.Timestamp
            ))
            .ToListAsync(cancellationToken);
    }
}
