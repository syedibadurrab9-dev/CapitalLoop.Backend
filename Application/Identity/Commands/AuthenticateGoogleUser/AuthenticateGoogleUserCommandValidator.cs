using FluentValidation;

namespace Application.Portfolios.Commands;

/// <summary>
/// Proactive structural contract validator safeguarding your authentication pipelines from empty injection vectors.
/// Intercepted concurrently by ValidationBehavior.cs to eliminate execution thread wastes [CL-102].
/// </summary>
public class AuthenticateGoogleUserCommandValidator : AbstractValidator<AuthenticateGoogleUserCommand>
{
    public AuthenticateGoogleUserCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage("Authentication failed: Federated identity token payload cannot be left unassigned.");

        // Maximum length protection block to discard overflow injection bounds early
        RuleFor(x => x.IdToken)
            .MaximumLength(16384)
            .WithMessage("Authentication failed: Outbound token payload limits exceeded safe formatting ranges.");
    }
}