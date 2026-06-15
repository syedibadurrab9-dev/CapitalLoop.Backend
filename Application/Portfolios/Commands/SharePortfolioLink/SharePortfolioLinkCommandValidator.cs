using FluentValidation;

namespace Application.Portfolios.Commands.SharePortfolioLink;

/// <summary>
/// Proactive pipeline validator enforcing validation boundaries and security lifespan constraints.
/// </summary>
public class SharePortfolioLinkCommandValidator : AbstractValidator<SharePortfolioLinkCommand>
{
    public SharePortfolioLinkCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Portfolio ownership verification context email is mandatory.")
            .EmailAddress().WithMessage("Owner identification coordinate must match standard RFC formats.");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("A destination recipient communication email must be explicitly supplied.")
            .EmailAddress().WithMessage("Recipient communication coordinate must match standard RFC formats.")
            .NotEqual(x => x.UserEmail).WithMessage("Security violation: Portfolios cannot be shared back natively to the originating owner context.");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("Lifespan configurations must track a positive chronological duration.")
            .LessThanOrEqualTo(30).WithMessage("Ecosystem Protection Bound: Outbound snapshot share access links are restricted to a maximum 30-day structural ceiling.");
    }
}