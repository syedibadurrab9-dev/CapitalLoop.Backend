using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Domain Entity Tracking Wealth Advisory Consultation Bookings.
/// Encapsulates lead generation funnels, booking invariants, time-slot scheduling constraints,
/// and enterprise workflow assignment tracking fields across retail and back-office nodes.
/// </summary>
public class AdvisorConsultation : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string ProspectName { get; private set; } = string.Empty;
    public string ProspectEmail { get; private set; } = string.Empty;
    public string TargetedAssetClass { get; private set; } = string.Empty;
    public DateTime RequestedCallSlotUtc { get; private set; }
    public bool IsAssignedToAgent { get; private set; }
    public string? AssignedAgentEmail { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private AdvisorConsultation() : base() { }

    // =============================================================================
    // 🚀 CLASS CONSTRUCTOR WITH INTEGRATED TELEMETRY
    // =============================================================================

    /// <summary>
    /// Factory Constructor enforcing strict domain invariants upon corporate lead ingestion.
    /// </summary>
    public AdvisorConsultation(string name, string email, string assetClass, DateTime requestedCallSlotUtc, string operatorEmail) : base()
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Prospect designation parameters cannot be null or empty strings.", nameof(name));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Inbound lead validation failed: Malformed or unassigned email structure captured.", nameof(email));

        if (string.IsNullOrWhiteSpace(assetClass))
            throw new ArgumentException("Targeted asset class classification bounds cannot be unassigned.", nameof(assetClass));

        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Domain Invariant Violation: Audit telemetry tracking requires a valid operator context.", nameof(operatorEmail));

        // 🛡️ TIMEZONE PROTECTION VALVE: Coerce incoming DateTime structures safely to structural UTC metrics
        var calculatedUtcSlot = requestedCallSlotUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(requestedCallSlotUtc, DateTimeKind.Utc)
            : requestedCallSlotUtc.ToUniversalTime();

        // 🚨 CRITICAL SCHEDULING RULE: Protect workspace calendars from corrupted historical assignment loops
        if (calculatedUtcSlot < DateTime.UtcNow.AddMinutes(5)) // Includes a safe 5-minute clock-skew allowance threshold
            throw new ArgumentException("Advisory schedule targets cannot register into historical time parameters or immediate windows.", nameof(requestedCallSlotUtc));

        ProspectName = name.Trim();
        ProspectEmail = email.Trim().ToLowerInvariant();
        TargetedAssetClass = assetClass.Trim();
        RequestedCallSlotUtc = calculatedUtcSlot;
        IsAssignedToAgent = false;

        // Populate absolute baseline creation footprints inside the inherited domain infrastructure tracking fields
        SetCreationAuditTelemetry(operatorEmail);
    }

    // =============================================================================
    // 🎛️ BULLETPROOF MUTATORS & STATE TRANSITION ENFORCEMENT
    // =============================================================================

    /// <summary>
    /// Pushes the lead entity state forward once an expert advisor claim handshake completes.
    /// Registers target specialist identity metadata to comply with compliance audits.
    /// </summary>
    /// <param name="agentEmail">The validated email identity coordinates of the enterprise specialist agent.</param>
    /// <param name="operatorEmail">The security principal context execution identity executing this appointment assignment.</param>
    public void AssignToConsultant(string agentEmail, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(agentEmail) || !agentEmail.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Assignment context rejected: Specialist identity coordinates are invalid.", nameof(agentEmail));

        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        if (IsAssignedToAgent)
            throw new InvalidOperationException($"Active consultation entity record is already bound to advisor workspace: {AssignedAgentEmail}");

        IsAssignedToAgent = true;
        AssignedAgentEmail = agentEmail.Trim().ToLowerInvariant();

        // Append context parameters cleanly into the persistent layer tracking fields via the security context
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Safely updates an upcoming appointment coordinate matrix while maintaining structural timeline checks.
    /// </summary>
    /// <param name="newSlotUtc">The new target time slot layout context.</param>
    /// <param name="operatorEmail">The security principal context execution identity executing this rescheduling modification.</param>
    public void RescheduleCallSlot(DateTime newSlotUtc, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        var calculatedUtcSlot = newSlotUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(newSlotUtc, DateTimeKind.Utc)
            : newSlotUtc.ToUniversalTime();

        if (calculatedUtcSlot < DateTime.UtcNow.AddMinutes(5))
            throw new ArgumentException("Reschedule target coordinates cannot register into historical or decaying timestamp allocations.", nameof(newSlotUtc));

        RequestedCallSlotUtc = calculatedUtcSlot;

        // Push explicit security claims markers down into the entity audit trail tracking layer
        MarkAsModified(operatorEmail);
    }
}