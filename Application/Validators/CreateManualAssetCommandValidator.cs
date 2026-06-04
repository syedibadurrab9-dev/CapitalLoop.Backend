using Application.Portfolios.Commands.CreateManualAsset;
using FluentValidation;

namespace Application.Validators;

public class CreateManualAssetCommandValidator : AbstractValidator<CreateManualAssetCommand>
{
    public CreateManualAssetCommandValidator()
    {
        RuleFor(v => v.UserEmail).NotEmpty().EmailAddress();
        RuleFor(v => v.Name).NotEmpty().MaximumLength(150);
        RuleFor(v => v.HoldingValuePKR).GreaterThan(0).WithMessage("Asset pricing units must remain completely positive parameters.");
    }
}