using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Commands.SubmitKycDocuments;

/// <summary>
/// ⚙️ CQRS COMMAND HANDLER ORCHESTRATION LAYER
/// Manages onshore regulatory compliance document submissions, coordinates state transitions 
/// across your central UserProfile aggregate, and registers physical identity documents for verification [CL-104].
/// </summary>
public class SubmitKycDocumentsCommandHandler : IRequestHandler<SubmitKycDocumentsCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public SubmitKycDocumentsCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> Handle(SubmitKycDocumentsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        // 🎯 STEP 1: PERSISTENCE ALIGNMENT - Fetch user profile context from storage to alter its state
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"Compliance error: Targeted user profile record could not be found for identity '{sanitizedEmail}'.");
        }

        // =============================================================================
        // 🧱 FIXES USERPROFILE CS7036: PASS COMPLIANCE OPERATOR FOOTPRINT TO MUTATOR
        // =============================================================================
        // Appended 'sanitizedEmail' as the third tracking argument to satisfy your UserProfile entity's audit constraints.
        profile.UpdateKyc(command.Cnic, command.Age, sanitizedEmail);

        // 🎯 STEP 2: REGULATORY LIFECYCLE INVARIANT - Query if an asset record already exists to prevent duplicate rows
        var existingDoc = await _context.KycDocuments
            .FirstOrDefaultAsync(d => d.UserEmail == sanitizedEmail, cancellationToken);

        if (existingDoc is not null)
        {
            // If it exists but was rejected previously, remove the old instance to accept the fresh tracking stream
            _context.KycDocuments.Remove(existingDoc);
        }

        // =============================================================================
        // 🧱 FIXES KYCDOCUMENT OVERLOAD MISMATCH: REALIGNED CONTEXT TO MATCH 5-PARAM FACTORY
        // =============================================================================
        // Removed the extra trailing argument to match your entity method signature exactly.
        var kycSubmission = KycDocument.RecordSubmission(
            userProfileId: profile.Id,
            email: sanitizedEmail,
            frontPath: command.FrontImagePath,
            backPath: command.BackImagePath,
            rawCnicText: command.Cnic
        );

        // Stage the fresh aggregate record across your synchronized database context collection 'KycDocuments'
        await _context.KycDocuments.AddAsync(kycSubmission, cancellationToken);

        // 🎯 STEP 4: TRANSACTION PERSISTENCE - Atomically commit state alterations down to storage layers
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}