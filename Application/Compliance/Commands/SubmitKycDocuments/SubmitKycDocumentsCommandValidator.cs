using System.Linq;
using FluentValidation;

namespace Application.Compliance.Commands.SubmitKycDocuments;

/// <summary>
/// Proactive pipeline validation layer executing structural rules on raw identity inputs before database entry.
/// </summary>
public class SubmitKycDocumentsCommandValidator : AbstractValidator<SubmitKycDocumentsCommand>
{
    public SubmitKycDocumentsCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Compliance tracking requires a valid user registration email context.")
            .EmailAddress().WithMessage("Target context email structure is invalid.");

        RuleFor(x => x.Cnic)
            .NotEmpty().WithMessage("Onshore verification parameters require a valid identification token.")
            .Must(cnic => cnic != null && cnic.All(char.IsDigit)).WithMessage("CNIC structural parsing exception: Input characters must consist purely of digits.")
            .Length(13).WithMessage("Onshore OCR verification criteria failed: Pakistani Smart CNIC must register exactly 13 numeric digits.");

        RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(18).WithMessage("SECP Suitability exception: Account operators must have reached legal majority age limits (18) to engage with ledger features.");
    }
}