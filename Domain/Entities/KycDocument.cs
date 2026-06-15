using System;
using System.Linq;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Domain Entity Governing Onshore KYC Document Verification Cycles [CL-104 / CL-601].
/// Coordinates regulatory customer onboarding data processing, 13-digit CNIC syntax compliance,
/// and back-office audit assignment properties across mobile and administration portals.
/// </summary>
public class KycDocument : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public Guid UserProfileId { get; private set; }

    // PROPERTY REALIGNMENT: Added user email property to enable direct lookup filtering strings
    public string UserEmail { get; private set; } = string.Empty;
    public string FrontImagePath { get; private set; } = string.Empty;
    public string BackImagePath { get; private set; } = string.Empty;
    public string SanitizedCnic { get; private set; } = string.Empty;
    public KycStatus Status { get; private set; } = KycStatus.Unverified;
    public string? RejectionReason { get; private set; }
    public string? AuditedByAgentEmail { get; private set; }
    public DateTime? AuditedAtUtc { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private KycDocument() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Factory initializer to record onshore identity captures flowing from KycScreen.tsx [CL-104].
    /// Enforces strict regulatory data structure syntax constraints prior to state pipeline submission.
    /// </summary>
    public static KycDocument RecordSubmission(Guid userProfileId, string email, string frontPath, string backPath, string rawCnicText)
    {
        if (userProfileId == Guid.Empty)
            throw new ArgumentException("KYC submissions must target a valid initialized UserProfile identifier.", nameof(userProfileId));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("KYC submissions must contain a valid customer communication contact email.", nameof(email));

        if (string.IsNullOrWhiteSpace(frontPath))
            throw new ArgumentException("Front physical document document-capture path must be supplied.", nameof(frontPath));

        if (string.IsNullOrWhiteSpace(backPath))
            throw new ArgumentException("Back physical document document-capture path must be supplied.", nameof(backPath));

        // 🛡️ SANITIZE INPUT: Strip unexpected spaces or hyphens to perform uniform calculation checks
        var cleanedCnic = new string(rawCnicText.Where(char.IsDigit).ToArray());

        // 🚨 PAKISTAN REGULATORY CHECK: Enforce strict structural 13-digit parameters 
        if (cleanedCnic.Length != 13)
            throw new ArgumentException("Extracted data structure does not match a valid 13-digit numerical CNIC identity token contract.", nameof(rawCnicText));

        var document = new KycDocument
        {
            Id = Guid.NewGuid(),
            UserProfileId = userProfileId,
            UserEmail = email.Trim().ToLowerInvariant(),
            FrontImagePath = frontPath.Trim(),
            BackImagePath = backPath.Trim(),
            SanitizedCnic = cleanedCnic,
            Status = KycStatus.Pending, // Transitions straight into processing queues upon submission
            RejectionReason = null
        };

        // Automate database validation audit footprints natively upon instance generation using client context
        document.SetCreationAuditTelemetry(document.UserEmail);

        return document;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF MUTATORS & COMPLIANCE STATE MACHINE
    // =============================================================================

    /// <summary>
    /// Promotes a user's compliance record inside the kyc-ledger system to an Approved state.
    /// </summary>
    /// <param name="agentEmail">The email tracking identity of the back-office compliance agent executing this review.</param>
    public void Approve(string agentEmail)
    {
        ValidateReviewerAgent(agentEmail);

        Status = KycStatus.Approved;
        RejectionReason = null;
        AuditedByAgentEmail = agentEmail.Trim().ToLowerInvariant();
        AuditedAtUtc = DateTime.UtcNow;

        // Push explicit security claims markers down into the entity audit trail tracking layer
        MarkAsModified(agentEmail);
    }

    /// <summary>
    /// Rejects the user's validation submission and locks down premium upgrade pipelines.
    /// </summary>
    /// <param name="agentEmail">The email tracking identity of the compliance agent executing this rejection review.</param>
    /// <param name="reason">The descriptive rejection reason code captured for administrative transparency.</param>
    public void Reject(string agentEmail, string reason)
    {
        ValidateReviewerAgent(agentEmail);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A formal rejection statement reason code must be documented for regulatory review histories.", nameof(reason));

        Status = KycStatus.Unverified;
        RejectionReason = reason.Trim();
        AuditedByAgentEmail = agentEmail.Trim().ToLowerInvariant();
        AuditedAtUtc = DateTime.UtcNow;

        // Push explicit security claims markers down into the entity audit trail tracking layer
        MarkAsModified(agentEmail);
    }

    // =============================================================================
    // 🧱 RESOLVES CS1501: RE-ALIGNED METHOD SIGNATURE OBO AUDITOR OVERLOADS
    // =============================================================================
    /// <summary>
    /// Wraps rejection routines for evaluate handlers to preserve multi-tenant telemetry tracking hooks.
    /// </summary>
    /// <param name="reason">The reason text explaining document omission parameters.</param>
    /// <param name="agentEmail">The tracking coordinate address of the active evaluation operator.</param>
    public void AppendRejectionAuditNotes(string reason, string? agentEmail = null)
    {
        // Fall back gracefully to system background service addresses if left unassigned
        var effectiveAgent = !string.IsNullOrWhiteSpace(agentEmail)
            ? agentEmail
            : "compliance-engine@capitalloop.com";

        Reject(effectiveAgent, reason);
    }

    /// <summary>
    /// Returns the standardized, hyphenated NADRA identity tracking layout configuration string (e.g., xxxxx-xxxxxxx-x).
    /// </summary>
    public string GetFormattedCnic()
    {
        if (string.IsNullOrWhiteSpace(SanitizedCnic) || SanitizedCnic.Length != 13)
            return SanitizedCnic;

        return $"{SanitizedCnic[..5]}-{SanitizedCnic[5..12]}-{SanitizedCnic[12]}";
    }

    // =============================================================================
    // 🧱 PRIVATE INVARIANT VALIDATORS
    // =============================================================================
    private void ValidateReviewerAgent(string agentEmail)
    {
        if (string.IsNullOrWhiteSpace(agentEmail) || !agentEmail.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Compliance tracking error: Auditor identity signature is invalid or empty.", nameof(agentEmail));

        if (Status == KycStatus.Approved)
            throw new InvalidOperationException("Modification blocked: Active validation record is already definitively verified and finalized.");
    }
}