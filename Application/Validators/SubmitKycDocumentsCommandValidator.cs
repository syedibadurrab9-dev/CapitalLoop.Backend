using Application.Portfolios.Commands.SubmitKycDocuments;
using FluentValidation;

namespace Application.Validators;

public class SubmitKycDocumentsCommandValidator : AbstractValidator<SubmitKycDocumentsCommand>
{
    public SubmitKycDocumentsCommandValidator()
    {
        RuleFor(v => v.UserEmail)
            .NotEmpty().WithMessage("User email address is mandatory for compliance recording.")
            .EmailAddress().WithMessage("Provided identity email must be a valid email structure.");

        RuleFor(v => v.DocumentType)
            .NotEmpty().WithMessage("Document type is required.")
            .Matches("^(Smart_CNIC|Passport)$")
            .WithMessage("Document type must strictly be 'Smart_CNIC' or 'Passport'.");

        When(v => v.DocumentType == "Smart_CNIC", () =>
        {
            RuleFor(v => v.DocumentNumber)
                .NotEmpty().WithMessage("Smart CNIC number is required.")
                .Matches(@"^\d{5}-\d{7}-\d{1}$")
                .WithMessage("Smart CNIC must conform to standard 13-digit Pakistani format (XXXXX-XXXXXXX-X).");
        });

        When(v => v.DocumentType == "Passport", () =>
        {
            RuleFor(v => v.DocumentNumber)
                .NotEmpty().WithMessage("Passport number is required.")
                .Matches(@"^[A-Z0-9]{7,9}$")
                .WithMessage("Passport number must contain 7 to 9 alphanumeric characters.");
        });

        RuleFor(v => v.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required for regulatory age verification.")
            .Must(BeAtLeast18YearsOld)
            .WithMessage("SECP and SBP regulatory policy: Investors must be at least 18 years old to open an investment profile.");
    }

    private static bool BeAtLeast18YearsOld(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age)) age--;
        return age >= 18;
    }
}
