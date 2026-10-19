using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Commands.SyncTaxFilerStatus;

public record FbrAtlSyncResultDto(
    string Email,
    bool IsFiler,
    string DocumentNumber,
    DateTime VerifiedAtUtc,
    string StatusDescription
);

public record SyncTaxFilerStatusCommand(string UserEmail) : ICommand<FbrAtlSyncResultDto>;

public class SyncTaxFilerStatusCommandHandler : ICommandHandler<SyncTaxFilerStatusCommand, FbrAtlSyncResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFbrTaxService _fbrTaxService;

    public SyncTaxFilerStatusCommandHandler(
        IApplicationDbContext context,
        IFbrTaxService fbrTaxService)
    {
        _context = context;
        _fbrTaxService = fbrTaxService;
    }

    public async Task<FbrAtlSyncResultDto> HandleAsync(SyncTaxFilerStatusCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        var user = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User profile for '{command.UserEmail}' was not found.");

        // Locate latest verified KYC document to retrieve 13-digit Smart CNIC
        var latestDoc = await _context.KycDocuments
            .Where(d => d.UserEmail == normalizedEmail)
            .OrderByDescending(d => d.SubmittedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var cnic = latestDoc?.DocumentNumber ?? string.Empty;

        // Query FBR Active Taxpayers List registry
        var fbrResult = await _fbrTaxService.CheckActiveTaxpayerStatusAsync(cnic, cancellationToken);

        // Idempotently update user profile filer status
        user.UpdateFilerStatus(fbrResult.IsActiveFiler);

        // SECP/FBR Compliance: Record immutable audit trail log
        var auditLog = ComplianceAuditLog.Create(
            targetEmail: normalizedEmail,
            reviewerAgentEmail: "fbr-atl-service@capitalloop.pk",
            actionPerformed: fbrResult.IsActiveFiler ? "FBR_ATL_FILER_VERIFIED" : "FBR_ATL_NON_FILER_FLAGGED",
            comments: $"FBR ATL verification status: {fbrResult.StatusDescription}. CNIC: {cnic}"
        );

        await _context.ComplianceAuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new FbrAtlSyncResultDto(
            Email: user.Email,
            IsFiler: user.IsFiler,
            DocumentNumber: cnic,
            VerifiedAtUtc: user.AtlVerifiedUtc ?? DateTime.UtcNow,
            StatusDescription: fbrResult.StatusDescription
        );
    }
}
