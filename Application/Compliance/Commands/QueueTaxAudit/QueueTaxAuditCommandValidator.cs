using FluentValidation;

namespace Application.Compliance.Commands.QueueTaxAudit;

/// <summary>
/// Proactive pipeline edge validator verifying structural compliance variables.
/// </summary>
public class QueueTaxAuditCommandValidator : AbstractValidator<QueueTaxAuditCommand>
{
    public QueueTaxAuditCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Tax evaluation sequencing requires a valid target account email context.")
            .EmailAddress().WithMessage("Target user identifier must match standard RFC validation layout specifications.");
    }
}