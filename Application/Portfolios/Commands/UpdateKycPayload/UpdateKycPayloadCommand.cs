using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Portfolios.Commands.UpdateKycPayload;

public record UpdateKycPayloadCommand(
    string UserEmail,
    string DocumentType,
    string DocumentNumber,
    string EncryptedStoragePath) : IRequest<bool>;

public class UpdateKycPayloadCommandHandler : IRequestHandler<UpdateKycPayloadCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateKycPayloadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateKycPayloadCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == request.UserEmail.ToLowerInvariant(), cancellationToken);

        if (profile is null) return false;

        // Formulate and record the secure domain entity tracking item
        var kycDoc = KycDocument.RecordSubmission(
            request.UserEmail,
            request.DocumentType,
            request.DocumentNumber,
            request.EncryptedStoragePath
        );

        _context.KycDocuments.Add(kycDoc);

        // Transition state variables immediately into review mode
        profile.UpdateKycStatus(KycStatus.Pending);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}