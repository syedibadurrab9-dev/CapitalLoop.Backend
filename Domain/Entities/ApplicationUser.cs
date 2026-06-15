using System;
using Domain.Common;
using Domain.Constants;

namespace Domain.Entities;

/// <summary>
/// 📱 CENTRAL IDENTITY USER CONTROLLER MODEL
/// Inherits structural DDD invariants from Entity base. Manages security principals, 
/// authentication parameters, and 5-tier membership privilege tracking across active platform channels [CL-503].
/// </summary>
public class ApplicationUser : Entity
{
    // =============================================================================
    // 🧱 SECURE IDENTITY PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================

    // Note: The unique Tracking ID is inherited as a Guid from the Entity base.
    // If wrapping an external string-based provider ID (e.g. Google Sub Claim ID),
    // we capture it explicitly below to decouple storage tracking keys from identity keys.
    public string IdentityProviderId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// 🚀 5-TIER ECOSYSTEM PRIVILEGE ASSIGNMENT [CL-503]
    /// Reflects authorized platform states: standard-free, premium, advisor-wealth, tax-consultant-tier, agent-backoffice.
    /// </summary>
    public string SubscriptionTier { get; private set; } = SubscriptionTiers.StandardFree;

    /// <summary>
    /// Gets whether the identity holds an active retail premium tier subscription layout context.
    /// </summary>
    public bool IsPremium => SubscriptionTier.Equals(SubscriptionTiers.Premium, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Enforces the architectural boundary requirement: standard-free and premium roles exclusively utilize React Native.
    /// </summary>
    public bool IsMobileAccessPermitted =>
        SubscriptionTier.Equals(SubscriptionTiers.StandardFree, StringComparison.OrdinalIgnoreCase) ||
        SubscriptionTier.Equals(SubscriptionTiers.Premium, StringComparison.OrdinalIgnoreCase);

    public DateTime SubscriptionUpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    // =============================================================================
    // 🔗 1-TO-1 AGGREGATE CORE RELATIONSHIP ANCHOR
    // =============================================================================
    public Guid? UserProfileId { get; private set; }
    public virtual UserProfile? Profile { get; private set; }

    /// <summary>
    /// Required constructor for Entity Framework Core hydration mechanics and Identity backing stores.
    /// </summary>
    public ApplicationUser() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS (GOOGLE SSO BOOTSTRAP)
    // =============================================================================

    /// <summary>
    /// Instantiates a type-safe security identity profile anchored to verified federated Google SSO authorization credentials.
    /// </summary>
    public static ApplicationUser CreateFromGoogleSso(string googleSubId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(googleSubId))
            throw new ArgumentException("Identity constructor failed: Federated Google Subject tracking keys cannot be empty.", nameof(googleSubId));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Identity constructor failed: Identity verification requires a valid target email structure.", nameof(email));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), // Absolute database surrogate tracking key
            IdentityProviderId = googleSubId.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "External Operator" : displayName.Trim(),
            SubscriptionTier = SubscriptionTiers.StandardFree,
            SubscriptionUpdatedAtUtc = DateTime.UtcNow
        };

        // Self-sign the initialization audit trail safely before persistence context attachments
        user.SetCreationAuditTelemetry(user.Email);

        return user;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF STATE MUTATORS & AUDIT LOG MATRIX
    // =============================================================================

    /// <summary>
    /// Modifies a user's system platform tier workspace role, enforcing absolute structural matching 
    /// against authorized multi-tier architecture constants [CL-503].
    /// </summary>
    /// <param name="targetTierToken">The authorized system tier role designation string.</param>
    /// <param name="operatorEmail">The email tracking identity of the administrator executing the override change.</param>
    public void ChangeSubscriptionTier(string targetTierToken, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(targetTierToken))
            throw new ArgumentException("Subscription mutation rejected: Target subscription tier parameters cannot match empty strings.", nameof(targetTierToken));

        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Subscription mutation rejected: Operator audit tracking context is mandatory.", nameof(operatorEmail));

        var formattedTier = targetTierToken.Trim().ToLowerInvariant();

        // 🚨 IDENTITY CORE INVARIANT: Enforce strict architectural configuration alignment
        if (!SubscriptionTiers.IsValidTier(formattedTier))
            throw new InvalidOperationException($"Security structure exception: System tier validation failed for target role value: '{targetTierToken}'.");

        // Short-circuit if assignment parameters mirror active state properties (Idempotency)
        if (SubscriptionTier == formattedTier) return;

        SubscriptionTier = formattedTier;
        SubscriptionUpdatedAtUtc = DateTime.UtcNow;

        // Persist explicit ABAC audit tracks downstream via audited entity mutation hooks
        MarkAsModified(operatorEmail);

        // 🚀 RELATIONSHIP CASCADE FIXED: Injected the required operator trace into the child aggregate root mutator loop
        if (Profile is not null && Profile.SubscriptionTier != formattedTier)
        {
            Profile.UpdateSubscriptionPlan(formattedTier, operatorEmail);
        }
    }

    /// <summary>
    /// Structural mutation method used to bind a newly created domain user profile aggregate record directly to this identity context.
    /// </summary>
    public void LinkUserProfile(UserProfile userProfile)
    {
        ArgumentNullException.ThrowIfNull(userProfile);

        if (Profile is not null && Profile.Id != userProfile.Id)
            throw new InvalidOperationException("Ecosystem entity mapping exception: This identity is already definitively bound.");

        Profile = userProfile;

        // Ensure the foreign key Guid is synchronized
        UserProfileId = userProfile.Id;

        MarkAsModified(this.Email);
    }
}