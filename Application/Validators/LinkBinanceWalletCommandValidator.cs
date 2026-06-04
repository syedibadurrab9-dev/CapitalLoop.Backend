using Application.Portfolios.Commands.LinkBinanceWallet;
using FluentValidation;

namespace Application.Validators;

public class LinkBinanceWalletCommandValidator : AbstractValidator<LinkBinanceWalletCommand>
{
    public LinkBinanceWalletCommandValidator()
    {
        RuleFor(v => v.ApiKey)
            .NotEmpty().WithMessage("Binance API Key string payload is required.")
            .MaximumLength(128).WithMessage("API Key exceeds expected cryptographic limits.");

        RuleFor(v => v.SecretKey)
            .NotEmpty().WithMessage("Binance Secret Key string parameter cannot be omitted.")
            .MaximumLength(128).WithMessage("Secret Key signature payload size constraints violated.");

        RuleFor(v => v.UserEmail)
            .NotEmpty().WithMessage("Security profile context email identity tracing reference is mandatory.")
            .EmailAddress().WithMessage("Provided transaction tracking context requires a valid email structure.");
    }
}