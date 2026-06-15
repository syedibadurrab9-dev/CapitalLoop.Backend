using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Commands.EvaluateKycAudit;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Command Handler processing explicit compliance decisions from back-office screens [CL-601].
/// Fully cross-linked with 'UserProfiles' and 'KycDocuments' collections specified in your master matrix.
/// </summary>
public class EvaluateKycAuditCommandHandler : IRequestHandler<EvaluateKycAuditCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public EvaluateKycAuditCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> Handle(EvaluateKycAuditCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Guard Invariant: Restrict arbitrary operator input tracking from forging illegal pending states
        if (command.NewStatus == KycStatus.Pending)
        {
            throw new InvalidOperationException("Compliance state failure: Administrative audit processes cannot manually force or re-queue an active tracking node into a Pending state.");
        }

        var sanitizedEmail = command.TargetUserEmail.Trim().ToLowerInvariant();

        // 🎯 STEP 1: DOMAIN PROFILE MUTATION - Locate and update the target customer identity aggregate status
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"Compliance mutation error: Target profile tracking record could not be found for identity '{sanitizedEmail}'.");
        }

        // =============================================================================
        // 🧱 RESOLVES CS7036: PASS COMPLIANCE AUDITOR FOOTPRINT TO MUTATOR
        // =============================================================================
        // Resolve the active compliance auditor email context driving this review process loop
        var activeAuditor = !string.IsNullOrWhiteSpace(command.AuditorEmail)
            ? command.AuditorEmail.Trim().ToLowerInvariant()
            : "compliance-backoffice-auditor@capitalloop.internal";

        // Apply state transition via domain aggregate mutator methods with explicit auditing safety
        profile.UpdateKycStatus(command.NewStatus, activeAuditor);

        // 🎯 STEP 2: METRIC DOCUMENT RECORDING - Query and append tracking logs over your synchronized 'KycDocuments' collection
        if (command.NewStatus == KycStatus.Unverified && !string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            var kycDoc = await _context.KycDocuments
                .FirstOrDefaultAsync(d => d.UserEmail == sanitizedEmail, cancellationToken);

            if (kycDoc is not null)
            {
                // Persist the explicit administrative rejection trail down to the underlying tracking row metadata
                // Passing the auditor footprint to preserve unified data trail telemetry across aggregate boundaries
                kycDoc.AppendRejectionAuditNotes(command.RejectionReason.Trim(), activeAuditor);
            }
        }

        // 🎯 STEP 3: TRANSACTIONAL PERSISTENCE - Commit compliance state mutations back down to physical SQL pools
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}