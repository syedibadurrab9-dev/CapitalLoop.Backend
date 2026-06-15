using FluentValidation;

namespace Application.Portfolios.Commands;

/// <summary>
/// Proactive pipeline edge validator verifying structural input criteria before initiating heavy score evaluation.
/// Intercepted concurrently by ValidationBehavior.cs to eliminate execution thread wastes [CL-102].
/// </summary>
public class EvaluateSuitabilityCommandValidator : AbstractValidator<EvaluateSuitabilityCommand>
{
    public EvaluateSuitabilityCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Suitability evaluations require an authenticated user identity.")
            .EmailAddress().WithMessage("Supplied identity coordinates must conform to standard RFC email format layouts.");

        RuleFor(x => x.QuestionResponses)
            .NotEmpty().WithMessage("The assessment scorecard dataset cannot be empty.")
            .Must(x => x.Count >= 3).WithMessage("Compliance Invariant Failure: You must supply answers to all fundamental risk profiling sections.");
    }
}