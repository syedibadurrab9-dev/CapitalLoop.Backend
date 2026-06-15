using System;
using FluentValidation;

namespace Application.Consultations.Commands.BookConsultationSlot;

/// <summary>
/// Proactive pipeline validator executing chronological constraints on inbound calendar slots.
/// </summary>
public class BookConsultationSlotCommandValidator : AbstractValidator<BookConsultationSlotCommand>
{
    public BookConsultationSlotCommandValidator()
    {
        RuleFor(x => x.ProspectName)
            .NotEmpty().WithMessage("Prospect designation name parameters cannot match empty sequences.")
            .MaximumLength(150).WithMessage("Prospect name length cannot exceed the maximum 150 character allocation limit.");

        RuleFor(x => x.ProspectEmail)
            .NotEmpty().WithMessage("Inbound lead validation failed: Contact email coordinates are required.")
            .EmailAddress().WithMessage("Prospect communication address must follow standard email validation schemas.");

        RuleFor(x => x.TargetedAssetClass)
            .NotEmpty().WithMessage("Targeted asset class configuration boundaries cannot be left unassigned.");

        RuleFor(x => x.RequestedCallSlotUtc)
            .Must(slot => slot > DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Advisory schedule targets cannot register into historical time parameters or decaying execution windows.");
    }
}