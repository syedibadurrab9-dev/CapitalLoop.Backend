using FluentValidation;

namespace Application.Portfolios.Commands.TriggerApiSync;

/// <summary>
/// Proactive edge validator preventing structural execution leaks or unassigned identifier calls.
/// </summary>
public class TriggerApiSyncCommandValidator : AbstractValidator<TriggerApiSyncCommand>
{
    public TriggerApiSyncCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Synchronization execution requires a valid tracking identity context.")
            .EmailAddress().WithMessage("User registration coordinate must match standard RFC email formats.");

        RuleFor(x => x.AssetNodeId)
            .NotEmpty().WithMessage("A target asset aggregate identifier node must be explicitly specified.");
    }
}