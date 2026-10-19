using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.UpdateKycPayload;

public record UpdateKycPayloadCommand(
    string UserEmail,
    string DocumentType,
    string DocumentNumber,
    string EncryptedStoragePath) : ICommand<bool>;

public class UpdateKycPayloadCommandHandler : ICommandHandler<UpdateKycPayloadCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdateKycPayloadCommand>? _validator;

    public UpdateKycPayloadCommandHandler(
        IApplicationDbContext context,
        IValidator<UpdateKycPayloadCommand>? validator = null)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<bool> HandleAsync(UpdateKycPayloadCommand command, CancellationToken cancellationToken)
    {
        if (_validator != null)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);
        }

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == command.UserEmail.ToLowerInvariant(), cancellationToken);

        if (profile is null) return false;

        // Formulate and record the secure domain entity tracking item
        var kycDoc = KycDocument.RecordSubmission(
            command.UserEmail,
            command.DocumentType,
            command.DocumentNumber,
            command.EncryptedStoragePath
        );

        _context.KycDocuments.Add(kycDoc);

        // Transition state variables immediately into review mode
        profile.UpdateKycStatus(KycStatus.Pending);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}