using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Consultations.Commands.BookConsultationSlot;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Command Handler processing domain factory validation rules and persisting early funnel lead records [CL-401].
/// Fully synchronized with the updated entity layer structures and standard data contexts.
/// </summary>
public class BookConsultationSlotCommandHandler : IRequestHandler<BookConsultationSlotCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public BookConsultationSlotCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Guid> Handle(BookConsultationSlotCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.ProspectEmail.Trim().ToLowerInvariant();

        // =============================================================================
        // 🧱 RESOLVES CS7036: PASS CONSTRUCTOR TELEMETRY FOOTPRINT TO ENTITY ENGINE
        // =============================================================================
        // Appended sanitizedEmail as the fifth argument to satisfy your aggregate root parameters.
        // If your Command contains an explicit executive actor or authenticated Admin identity, 
        // you can alternate this parameter to point directly to that token coordinate instead.
        var consultation = new AdvisorConsultation(
            name: command.ProspectName,
            email: sanitizedEmail,
            assetClass: command.TargetedAssetClass,
            requestedCallSlotUtc: command.RequestedCallSlotUtc,
            operatorEmail: sanitizedEmail
        );

        // 🎯 PERSISTENCE ALIGNMENT: Staged over your synchronized context tracking dataset 'AdvisorConsultations'
        await _context.AdvisorConsultations.AddAsync(consultation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return consultation.Id;
    }
}