using Application.Portfolios.Commands.UpdateKycPayload;
using FluentValidation;

namespace Application.Validators;

public class UpdateKycPayloadCommandValidator : AbstractValidator<UpdateKycPayloadCommand>
{
    public UpdateKycPayloadCommandValidator()
    {
        RuleFor(v => v.UserEmail).NotEmpty().EmailAddress();
        RuleFor(v => v.DocumentType).NotEmpty().Matches("^(Smart_CNIC|Passport)$")
            .WithMessage("Identity classification parameters must strictly contain approved compliance types.");
        RuleFor(v => v.DocumentNumber).NotEmpty().MinimumLength(5).MaximumLength(50);
        RuleFor(v => v.EncryptedStoragePath).NotEmpty();
    }
}
