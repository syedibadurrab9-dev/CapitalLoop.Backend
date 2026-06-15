using FluentValidation;

namespace Application.Portfolios.Commands.LinkBinanceWallet;

/// <summary>
/// Proactive structural validator safeguarding integration endpoints from malformed parameters.
/// </summary>
public class LinkBinanceWalletCommandValidator : AbstractValidator<LinkBinanceWalletCommand>
{
    public LinkBinanceWalletCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("A valid user profile context email must be supplied.")
            .EmailAddress().WithMessage("Target user account identifier must conform to standard RFC format layouts.");

        RuleFor(x => x.ApiKey)
            .NotEmpty().WithMessage("The external exchange API Key string context cannot be empty.")
            .Length(64).WithMessage("Cryptographic alignment fault: Binance public API key tokens must register exactly 64 characters.");

        RuleFor(x => x.SecretKey)
            .NotEmpty().WithMessage("The cryptographic exchange secret signing key parameter is mandatory.")
            .Length(64).WithMessage("Cryptographic alignment fault: Binance secret signing keys must register exactly 64 characters.");
    }
}