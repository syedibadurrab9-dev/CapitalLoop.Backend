using System;
using System.Collections.Generic;

namespace Application.Common.DTOs;

/// <summary>
/// Encapsulates the cryptographic identity token payload, profile metadata, 
/// and authorization access rules issued following a validated Google SSO login exchange [CL-101].
/// </summary>
public record AuthResultDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string IdentityEmail { get; init; } = string.Empty;
    public string DisplayMoniker { get; init; } = string.Empty;
    public string CurrentAssignedTier { get; init; } = string.Empty;
    public bool IsProfileKycVerified { get; init; }
    public DateTime TokenExpirationTimestampUtc { get; init; }

    public AuthResultDto() { }

    public AuthResultDto(string accessToken, string identityEmail, string displayMoniker, string currentAssignedTier, bool isProfileKycVerified, DateTime tokenExpirationTimestampUtc)
    {
        AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        IdentityEmail = identityEmail?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(identityEmail));
        DisplayMoniker = string.IsNullOrWhiteSpace(displayMoniker) ? "External Operator" : displayMoniker.Trim();
        CurrentAssignedTier = currentAssignedTier?.Trim().ToLowerInvariant() ?? "standard-free";
        IsProfileKycVerified = isProfileKycVerified;
        TokenExpirationTimestampUtc = tokenExpirationTimestampUtc;
    }
}