using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Domain Entity tracking cryptographically signed, read-only portfolio access links [CL-209].
/// </summary>
public class PortfolioShareLink : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string OwnerEmail { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string AccessToken { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    // Parameterless constructor required by EF Core deserialization mechanisms
    private PortfolioShareLink() : base() { }

    public PortfolioShareLink(string ownerEmail, string recipientEmail, string accessToken, DateTime expiresAtUtc) : base()
    {
        if (string.IsNullOrWhiteSpace(ownerEmail))
            throw new ArgumentException("Domain Invariant Violation: Portfolio share owner email context is mandatory.", nameof(ownerEmail));

        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("Domain Invariant Violation: Recipient target email context is mandatory.", nameof(recipientEmail));

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Domain Invariant Violation: High-entropy security access token must be supplied.", nameof(accessToken));

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("Domain Invariant Violation: Share expiration dates must settle into future chronological windows.", nameof(expiresAtUtc));

        Id = Guid.NewGuid(); // Overrides base entity placeholder with an explicit unique identity
        OwnerEmail = ownerEmail.Trim().ToLowerInvariant();
        RecipientEmail = recipientEmail.Trim().ToLowerInvariant();
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        IsRevoked = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Evaluates if the shared access link has expired or has been administrative revoked.
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;

    /// <summary>
    /// Forces an immediate manual revocation of this sharing permission token.
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }
}