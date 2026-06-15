using FluentValidation;

namespace Application.Chat.Commands.SendChatMessage;

/// <summary>
/// Proactive structural validator ensuring incoming chat payloads match strict security boundaries.
/// </summary>
public class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.SenderEmail)
            .NotEmpty().WithMessage("Message transmission sources must specify a sender email address.")
            .EmailAddress().WithMessage("Sender identity coordinate must follow standard email validation schemas.");

        RuleFor(x => x.ReceiverEmail)
            .NotEmpty().WithMessage("Message transmission targets must specify a receiver email address.")
            .EmailAddress().WithMessage("Receiver identity coordinate must follow standard email validation schemas.")
            .NotEqual(x => x.SenderEmail).WithMessage("Conversational state failure: Message sender coordinates cannot mirror receiver destinations.");

        RuleFor(x => x.MessageText)
            .MaximumLength(2000).WithMessage("Message content length cannot exceed the maximum 2,000 character allocation threshold.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.MessageText) || !string.IsNullOrWhiteSpace(x.ChartTokenPayload))
            .WithMessage("Dispatched interaction instances cannot transmit entirely empty payloads.");
    }
}