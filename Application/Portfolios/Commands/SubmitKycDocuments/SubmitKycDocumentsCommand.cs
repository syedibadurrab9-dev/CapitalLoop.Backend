using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Commands.SubmitKycDocuments;

public record SubmitKycDocumentsCommand(
    string UserEmail,
    string DocumentType,
    string DocumentNumber,
    DateOnly DateOfBirth,
    string? DocumentFileBase64 = null
) : ICommand<KycSubmissionResultDto>;

public record KycSubmissionResultDto(
    Guid DocumentId,
    KycStatus Status,
    string StoragePath,
    string Message
);

public class SubmitKycDocumentsCommandHandler : ICommandHandler<SubmitKycDocumentsCommand, KycSubmissionResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<SubmitKycDocumentsCommand>? _validator;
    private readonly ILogger<SubmitKycDocumentsCommandHandler> _logger;

    public SubmitKycDocumentsCommandHandler(
        IApplicationDbContext context,
        IValidator<SubmitKycDocumentsCommand>? validator = null,
        ILogger<SubmitKycDocumentsCommandHandler>? logger = null)
    {
        _context = context;
        _validator = validator;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SubmitKycDocumentsCommandHandler>.Instance;
    }

    public async Task<KycSubmissionResultDto> HandleAsync(SubmitKycDocumentsCommand command, CancellationToken cancellationToken)
    {
        // 1. FluentValidation check
        if (_validator != null)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);
        }

        // 2. Strict SECP Underage Invariant Verification (< 18 Years)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - command.DateOfBirth.Year;
        if (command.DateOfBirth > today.AddYears(-age)) age--;

        if (age < 18)
        {
            _logger.LogWarning("[KYC REJECTED] User {Email} failed underage invariant. Age: {Age}", command.UserEmail, age);
            throw new UnderageInvestorException($"SECP regulatory mandate: Investors must be at least 18 years of age. Current calculated age: {age}.");
        }

        // 3. User Profile Resolution
        var standardizedEmail = command.UserEmail.Trim().ToLowerInvariant();
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == standardizedEmail, cancellationToken);

        if (profile == null)
        {
            profile = UserProfile.Register(standardizedEmail.Split('@')[0], standardizedEmail);
            await _context.UserProfiles.AddAsync(profile, cancellationToken);
        }

        // 4. Generate PDPB Local Sovereign Storage Path
        var securePath = $"sovereign-storage/kyc/{standardizedEmail}/{Guid.NewGuid():N}_{command.DocumentType}.enc";

        // 5. Create KycDocument Entity Record
        var kycDoc = KycDocument.RecordSubmission(
            standardizedEmail,
            command.DocumentType,
            command.DocumentNumber,
            securePath
        );

        _context.KycDocuments.Add(kycDoc);

        // 6. Transition State to Pending Review
        profile.UpdateKycStatus(KycStatus.Pending);

        await _context.SaveChangesAsync(cancellationToken);

        var maskedDocNum = MaskDocumentNumber(command.DocumentNumber);
        _logger.LogInformation("[KYC AUDIT] Compliance document successfully submitted for {Email}. DocType: {Type}, Masked: {Masked}. Status: Pending.",
            standardizedEmail, command.DocumentType, maskedDocNum);

        return new KycSubmissionResultDto(
            kycDoc.Id,
            KycStatus.Pending,
            securePath,
            "Identity verification documents successfully filed for SECP sandbox compliance review."
        );
    }

    private static string MaskDocumentNumber(string docNum)
    {
        if (string.IsNullOrWhiteSpace(docNum) || docNum.Length < 7)
            return "****";

        return $"{docNum[..5]}-*******-{docNum[^1..]}";
    }
}
