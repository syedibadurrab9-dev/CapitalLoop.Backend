using Application.Common.Interfaces;
using Application.Common.Interfaces.ComplianceAsyncJobs;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Compliance.Commands.QueueTaxAudit;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Registers regulatory tax audit tasks into onshore database partitions and enqueues them 
/// onto high-speed memory channels for decentralized off-thread background computation [CL-104 / CL-501].
/// </summary>
public class QueueTaxAuditCommandHandler : IRequestHandler<QueueTaxAuditCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITaxAuditChannelProducer _auditChannelProducer;
    private readonly ILogger<QueueTaxAuditCommandHandler> _logger;

    public QueueTaxAuditCommandHandler(
        IApplicationDbContext context,
        ITaxAuditChannelProducer auditChannelProducer,
        ILogger<QueueTaxAuditCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditChannelProducer = auditChannelProducer ?? throw new ArgumentNullException(nameof(auditChannelProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(QueueTaxAuditCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Initializing Regulatory Tax Audit Entry | Profile Target: {Email} | System Triggered: {SysTrigger}",
            sanitizedEmail, command.IsTriggeredBySystem);

        // 1. Verify the profile exists before staging compute-heavy audit records
        var profileExists = await _context.UserProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (!profileExists)
            throw new InvalidOperationException($"Tax Audit Generation Rejected: Profile tracking context could not be resolved for identity '{sanitizedEmail}'.");

        // 🛡️ RESOLVES CS7036: Extract context footprint to fulfill domain validation audit constraints.
        // If the job was kicked off automatically by background system daemons, assign the system tag wrapper context.
        var effectiveOperator = command.IsTriggeredBySystem
            ? "system-compliance-scheduler@capitalloop.internal"
            : (!string.IsNullOrWhiteSpace(command.InitiatedByEmail) ? command.InitiatedByEmail.Trim().ToLowerInvariant() : sanitizedEmail);

        // =============================================================================
        // 🧱 2. INSTANTIATE DOMAIN ENTITY WITH SYSTEM METADATA AUDITING TRACKS
        // =============================================================================
        // Supplied effectiveOperator as the third argument to satisfy your constructor aggregate root invariants
        var taxAuditJob = new TaxAuditJob(
            userEmail: sanitizedEmail,
            initiatedBySystem: command.IsTriggeredBySystem,
            operatorEmail: effectiveOperator
        );

        await _context.TaxAuditJobs.AddAsync(taxAuditJob, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tax Optimization Audit Record Persisted | Job ID: {JobId}. Streaming to async background worker channels...",
            taxAuditJob.Id);

        // =============================================================================
        // 🎯 3. ASYNC QUEUE PRODUCER ENGINES LINK
        // =============================================================================
        // Dispatches the background worker process cleanly using exact interface named arguments
        try
        {
            await _auditChannelProducer.QueueAuditTaskAsync(
                jobId: taxAuditJob.Id,
                userEmail: sanitizedEmail,
                initiatedBySystem: command.IsTriggeredBySystem,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Transactional Outbox Mismatch: Record saved but background engine failed to queue task for Job {JobId}", taxAuditJob.Id);
        }

        return taxAuditJob.Id;
    }
}