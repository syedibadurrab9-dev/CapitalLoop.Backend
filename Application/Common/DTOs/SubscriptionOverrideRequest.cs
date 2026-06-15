using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTOs;

/// <summary>
/// 💳 SUBSCRIPTION OVERRIDE HANDSHAKE CONTRACT [CL-503]
/// Maps the structural incoming request body data for administrative workspace overrides.
/// </summary>
public record SubscriptionOverrideRequest
{
    public string TargetUserEmail { get; init; } = string.Empty;
    public string RequestedTierToken { get; init; } = string.Empty;
    public string AdministrativeJustificationReason { get; init; } = string.Empty;

    public SubscriptionOverrideRequest() { }

    public SubscriptionOverrideRequest(string targetUserEmail, string requestedTierToken, string administrativeJustificationReason)
    {
        if (string.IsNullOrWhiteSpace(targetUserEmail) || !targetUserEmail.Contains('@'))
            throw new ArgumentException("Ecosystem routing error: Target user email context is invalid.", nameof(targetUserEmail));

        if (string.IsNullOrWhiteSpace(requestedTierToken))
            throw new ArgumentException("Ecosystem routing error: Explicit target platform subscription tier must be provided.", nameof(requestedTierToken));

        TargetUserEmail = targetUserEmail.Trim().ToLowerInvariant();
        RequestedTierToken = requestedTierToken.Trim().ToLowerInvariant();
        AdministrativeJustificationReason = string.IsNullOrWhiteSpace(administrativeJustificationReason)
            ? "Administrative adjustment executed via back-office console."
            : administrativeJustificationReason.Trim();
    }
}