using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Commands.EvaluateKycAudit;

public record EvaluateKycAuditCommand(
    string TargetEmail,
    string ReviewerAgentEmail,
    string Status,
    string? Comments = null
) : ICommand<bool>;

public class EvaluateKycAuditCommandHandler : ICommandHandler<EvaluateKycAuditCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public EvaluateKycAuditCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HandleAsync(EvaluateKycAuditCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.TargetEmail.Trim().ToLowerInvariant();
        var user = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
            return false;

        // Parse and transition status
        var parsedStatus = command.Status.ToUpperInvariant() switch
        {
            "APPROVED" or "KYC_APPROVE" => KycStatus.Approved,
            "UNVERIFIED" or "REJECTED" or "KYC_REJECT" => KycStatus.Unverified,
            "PENDING" => KycStatus.Pending,
            _ => KycStatus.Pending
        };

        user.UpdateKycStatus(parsedStatus);

        // SECP Regulatory Compliance: Record immutable audit trail log
        var action = command.Status.ToUpperInvariant() switch
        {
            "APPROVED" => "KYC_APPROVE",
            "UNVERIFIED" or "REJECTED" => "KYC_REJECT",
            _ => command.Status.ToUpperInvariant()
        };

        var auditLog = ComplianceAuditLog.Create(
            targetEmail: normalizedEmail,
            reviewerAgentEmail: command.ReviewerAgentEmail,
            actionPerformed: action,
            comments: command.Comments ?? $"SECP review decision: {action}"
        );

        await _context.ComplianceAuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
