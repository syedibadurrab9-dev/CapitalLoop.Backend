using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Common;
using Domain.Constants;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Central Domain Aggregate Root Governing User Profiles across CapitalLoop Systems [CL-101].
/// Orchestrates cross-tier workspace parameters, onshore regulatory compliance parameters,
/// active FBR ATL tax filer indicators, SECP suitability scores, and ledger asset distribution node anchors.
/// </summary>
public class UserProfile : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Cnic { get; private set; }
    public bool IsFiler { get; private set; }
    public int Age { get; private set; }

    public string SubscriptionTier { get; private set; } = SubscriptionTiers.StandardFree;
    public KycStatus KycStatus { get; private set; } = KycStatus.Unverified;

    public RiskToleranceMatrix RiskMatrix { get; private set; } = RiskToleranceMatrix.Unassigned;
    public int SuitabilityAssessmentScore { get; private set; }
    public DateTime? SuitabilityAssessmentCompletedAtUtc { get; private set; }

    // =============================================================================
    // 🚀 1-TO-1 SECURITY IDENTITY FOREIGN KEY INTEGRATIONS
    // =============================================================================
    public Guid ApplicationUserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    // Navigation property mapping back to user structural asset node collections
    private readonly List<AssetNode> _assetNodes = new();
    public virtual IReadOnlyCollection<AssetNode> AssetNodes => _assetNodes.AsReadOnly();

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// 
    /// 
    public static UserProfile Null => new()
    {
        Id = Guid.Empty,
        Name = "Unknown User",
        Email = "null@capitalloop.local",
        SubscriptionTier = SubscriptionTiers.StandardFree,
        KycStatus = KycStatus.Unverified,
        RiskMatrix = RiskToleranceMatrix.Unassigned,
        IsFiler = false,
        Age = 0,
        SuitabilityAssessmentScore = 0,
        ApplicationUserId = Guid.Empty
    };

    /// </summary>
    private UserProfile() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Instantiates a core user record context boundary upon successful Google SSO identity exchanges [CL-101].
    /// </summary>
    public static UserProfile Register(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User profile registration failed: Moniker or name references cannot match empty sequences.", nameof(name));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("User profile registration failed: Target profile email format structural validation failed.", nameof(email));

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            SubscriptionTier = SubscriptionTiers.StandardFree,
            KycStatus = KycStatus.Unverified,
            RiskMatrix = RiskToleranceMatrix.Unassigned,
            IsFiler = false,
            SuitabilityAssessmentScore = 0
        };

        // Self-sign the initialization audit trail using the user's primary identity email
        profile.SetCreationAuditTelemetry(profile.Email);

        return profile;
    }

    /// <summary>
    /// Instantiates an administrative, back-office or specialized role profile context safely bypassing standard object initialization blockers.
    /// </summary>
    public static UserProfile RegisterAdmin(string name, string email, string subscriptionTier)
    {
        if (string.IsNullOrWhiteSpace(subscriptionTier))
            throw new ArgumentException("Administrative profile generation failed: Targeted operational tier indicator cannot be empty.", nameof(subscriptionTier));

        var formattedTier = subscriptionTier.Trim().ToLowerInvariant();

        // Validate administrative assignments against the 5-tier constant registry engine
        if (!SubscriptionTiers.IsValidTier(formattedTier))
            throw new InvalidOperationException($"Security override exception: System level role target '{subscriptionTier}' is invalid under current 5-tier platform rules.");

        var profile = Register(name, email);
        profile.SubscriptionTier = formattedTier;

        // Force system override telemetry traces
        profile.SetCreationAuditTelemetry("system-backend-bootstrap@capitalloop.local");

        return profile;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF STATE MUTATORS & INVARIANT VALIDATORS
    // =============================================================================

    /// <summary>
    /// Updates customer identity documents using strict regulatory input filters [CL-104].
    /// </summary>
    public void UpdateKyc(string rawCnicText, int accountAge, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(rawCnicText))
            throw new ArgumentException("Onshore verification failed: Identification text bounds cannot be empty.", nameof(rawCnicText));

        // 🛡️ SANITIZE INPUT: Uniform digit extraction
        var cleanedCnic = new string(rawCnicText.Where(char.IsDigit).ToArray());

        if (cleanedCnic.Length != 13)
            throw new ArgumentException("Onshore OCR validation exception: Pakistani Smart CNIC must measure exactly 13 numeric digits.", nameof(rawCnicText));

        // 🚨 SECP COMPLIANCE GATEWAY: Block financial execution structures for underage operators (must be 18+)
        if (accountAge < 18)
            throw new ArgumentOutOfRangeException(nameof(accountAge), "SECP Suitability exception: Account operators must have reached legal maturity age limits (18).");

        Cnic = cleanedCnic;
        Age = accountAge;
        KycStatus = KycStatus.Pending;

        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Implements regulatory SECP risk distribution models based on multi-asset density assessments [CL-302].
    /// </summary>
    public void RecordSuitabilityAssessment(string riskLevel, int calculationScore, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(riskLevel))
            throw new ArgumentException("The assigned structural risk tier evaluation text specification cannot be empty.", nameof(riskLevel));

        if (calculationScore < 0)
            throw new ArgumentException("The accumulated questionnaire calculation evaluation metric cannot be negative.", nameof(calculationScore));

        var targetRisk = riskLevel.Trim().ToLowerInvariant() switch
        {
            "conservative" or "low" => RiskToleranceMatrix.Conservative,
            "moderate" or "balanced" => RiskToleranceMatrix.Balanced,
            "aggressive" or "growth" => RiskToleranceMatrix.Growth,
            _ => throw new ArgumentException($"Invalid regulatory risk level structure context supplied: '{riskLevel}'", nameof(riskLevel))
        };

        // 🚨 SECP SUITABILITY AGE-CAPPING INVARIANT RULE [CL-301/CL-302]
        // If an account investor has crossed the maturity age limits threshold (e.g. 45 or 60+), 
        // high-volatility parameters are restricted, forcing an automatic down-scale allocation override to preserve capital.
        if (Age >= 45 && targetRisk == RiskToleranceMatrix.Growth)
        {
            targetRisk = RiskToleranceMatrix.Balanced;
        }
        if (Age >= 60)
        {
            targetRisk = RiskToleranceMatrix.Conservative;
        }

        RiskMatrix = targetRisk;
        SuitabilityAssessmentScore = calculationScore;
        SuitabilityAssessmentCompletedAtUtc = DateTime.UtcNow;

        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Configures Active Taxpayer List (ATL) filing parameters returned from background scrapers.
    /// </summary>
    public void SetFilerStatus(bool status, string operatorEmail)
    {
        if (IsFiler == status) return; // Idempotency protection check

        IsFiler = status;
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Modifies user privilege boundaries, enforcing structural alignment with the platform's 5-tier architecture invariants.
    /// </summary>
    public void UpdateSubscriptionPlan(string targetTierToken, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(targetTierToken))
            throw new ArgumentException("Target subscription plan workspace level configuration indicators cannot be blank.", nameof(targetTierToken));

        var formattedTier = targetTierToken.Trim().ToLowerInvariant();

        // 🚨 CORE ROLE INVARIANT: Intercept invalid subscription overrides before database pollution occurs
        if (!SubscriptionTiers.IsValidTier(formattedTier))
            throw new InvalidOperationException($"Security override exception: System level role target '{targetTierToken}' is invalid under current 5-tier platform rules.");

        SubscriptionTier = formattedTier;
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Moves the KYC process step forward using enum-driven state transitions.
    /// </summary>
    public void UpdateKycStatus(KycStatus status, string operatorEmail)
    {
        if (KycStatus == status) return; // Idempotency protection check

        KycStatus = status;
        MarkAsModified(operatorEmail);
    }
}