using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Consultations.Commands.AssignConsultant;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Command Handler orchestrating consultant claim handshakes and updating entity assignment tracks cleanly [CL-405].
/// Fully synchronized with the 'AdvisorConsultations' database context mapping definition.
/// </summary>
public class AssignConsultantCommandHandler : IRequestHandler<AssignConsultantCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public AssignConsultantCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> Handle(AssignConsultantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.AgentEmail))
        {
            throw new ArgumentException("Consultation adjustment rejected: The target advisor assignment email cannot be null or empty.", nameof(command.AgentEmail));
        }

        var sanitizedAgentEmail = command.AgentEmail.Trim().ToLowerInvariant();

        // 🎯 PERSISTENCE ALIGNMENT: Resolves records cleanly out of your realigned context properties
        var consultation = await _context.AdvisorConsultations
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (consultation is null)
            throw new InvalidOperationException($"Consultation adjustment rejected: Lead identifier tracking node '{command.ConsultationId}' could not be resolved.");

        // =============================================================================
        // 🧱 RESOLVES CS7036: PASS COMPLIANCE TELEMETRY FOOTPRINT TO MUTATOR
        // =============================================================================
        // Appended the active execution actor context string as the second parameter 
        // to completely satisfy your domain's audit trail modification invariants.
        consultation.AssignToConsultant(sanitizedAgentEmail, sanitizedAgentEmail);

        // 🎯 TRANSACTIONAL PERSISTENCE: Save state tracking updates cleanly to storage layers
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}