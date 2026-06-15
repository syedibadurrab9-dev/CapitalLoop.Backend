using FluentValidation;

namespace Application.Portfolios.Commands;

/// <summary>
/// Proactive pipeline edge validator ensuring incoming profile properties match core requirements.
/// Intercepted concurrently by ValidationBehavior.cs to eliminate execution thread wastes [CL-102].
/// </summary>
public class InitializeOnboardingCommandValidator : AbstractValidator<InitializeOnboardingCommand>
{
    public InitializeOnboardingCommandValidator()
    {
        RuleFor(x => x.ApplicationUserId)
            .NotEmpty()
            .WithMessage("Security link enforcement requires a valid ApplicationUserId root key tracking parameter.");

        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Workspace initialization requires an authenticated user email identity.")
            .EmailAddress().WithMessage("Supplied identity coordinates must conform to standard RFC email format layouts.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Profile generation requires a valid name descriptor string component.")
            .MaximumLength(100).WithMessage("The consolidated name string field values cannot exceed a 100-character threshold limit.");
    }
}