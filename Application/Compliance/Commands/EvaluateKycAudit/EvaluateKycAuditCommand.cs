using System;
using Domain.Enums;
using MediatR;

namespace Application.Compliance.Commands.EvaluateKycAudit;

/// <summary>
/// CQRS Command used by back-office compliance personnel to approve or decline a pending onboarding request [CL-104].
/// </summary>
/// <param name="TargetUserEmail">The email address of the consumer profile being evaluated.</param>
/// <param name="NewStatus">The final operational destination boundary assignment (Approved or Unverified).</param>
/// <param name="RejectionReason">The optional localized error string explaining a registry mismatch or structural failure.</param>
/// <param name="AuditorEmail">The active identity trace of the compliance auditor executing this verification process loop.</param>
public record EvaluateKycAuditCommand(
    string TargetUserEmail,
    KycStatus NewStatus,
    string? RejectionReason = null,
    string? AuditorEmail = null) : IRequest<bool>;