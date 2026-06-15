using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Domain Entity tracking the lifecycle and state transitions of background tax optimization audits [CL-104].
/// Coordinates multi-tenant task indicators processed sequentially off-thread via memory-decoupled channels.
/// </summary>
public class TaxAuditJob : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string UserEmail { get; private set; } = string.Empty;
    public bool InitiatedBySystem { get; private set; }
    public string ExecutionStatus { get; private set; } = "Queued"; // Queued, Processing, Completed, Faulted
    public DateTime? ProcessedAtUtc { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private TaxAuditJob() : base() { }

    // =============================================================================
    // 🚀 CLASS CONSTRUCTOR WITH INTEGRATED TELEMETRY
    // =============================================================================
    public TaxAuditJob(string userEmail, bool initiatedBySystem, string operatorEmail) : base()
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            throw new ArgumentException("Domain Invariant Violation: Tax audit target user email identity is mandatory.", nameof(userEmail));

        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Domain Invariant Violation: Audit telemetry tracking requires a valid operator context.", nameof(operatorEmail));

        Id = Guid.NewGuid();
        UserEmail = userEmail.Trim().ToLowerInvariant();
        InitiatedBySystem = initiatedBySystem;
        ExecutionStatus = "Queued";

        // Populate absolute baseline creation footprints inside the inherited domain infrastructure tracking fields
        SetCreationAuditTelemetry(operatorEmail);
    }

    // =============================================================================
    // 🎛️ BULLETPROOF MUTATORS & SYSTEM STATE MACHINE
    // =============================================================================

    /// <summary>
    /// Updates the job status indicators securely when an isolated single-reader background daemon worker picks it up.
    /// </summary>
    /// <param name="operatorEmail">The system background service identity or consultant claim string driving the thread execution loop.</param>
    public void MarkAsProcessing(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("State mutation tracking requires an authorized operator footprint.", nameof(operatorEmail));

        if (ExecutionStatus == "Completed")
            throw new InvalidOperationException("Ecosystem Exception: Finalized tax job lifecycles cannot be reverted to a processing state.");

        ExecutionStatus = "Processing";

        // Append context parameters cleanly into the persistent layer track fields
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Finalizes the audit lifecycle track when processing calculations conclude cleanly.
    /// </summary>
    /// <param name="operatorEmail">The system background service identity or consultant claim string finalization trigger.</param>
    public void CompleteJob(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("State finalization tracking requires an authorized operator footprint.", nameof(operatorEmail));

        ExecutionStatus = "Completed";
        ProcessedAtUtc = DateTime.UtcNow;

        // Append context parameters cleanly into the persistent layer track fields
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Flags task streams that abort due to exception cascades or upstream scraper timeouts.
    /// </summary>
    /// <param name="operatorEmail">The system fault trace logger identity.</param>
    public void MarkAsFaulted(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Fault monitoring state registration requires an operator context.", nameof(operatorEmail));

        ExecutionStatus = "Faulted";
        ProcessedAtUtc = DateTime.UtcNow;

        MarkAsModified(operatorEmail);
    }
}