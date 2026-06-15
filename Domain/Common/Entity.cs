using System;
using System.Collections.Generic;

namespace Domain.Common;

/// <summary>
/// 🧱 DOMAIN-DRIVEN DESIGN (DDD) ABSTRACT BASE ENTITY CONTRACT
/// Provides absolute architectural identity boundaries, thread-protected domain event dispatch queues, 
/// and reference-safe mutation tracking invariants for CapitalLoop database engines.
/// </summary>
public abstract class Entity
{
    private readonly List<object> _domainEvents = new();

    /// <summary>
    /// Anchors the structural tracking identity across database storage limits.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Infrastructure lifecycle timestamp critical for evaluating cold-start synchronization or cache invalidation delays.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// 🔒 SECURITY ABAC ATTRIBUTE: Captures the Google SSO authenticated email of the profile creator.
    /// </summary>
    public string CreatedByEmail { get; protected set; } = string.Empty;

    /// <summary>
    /// Tracking audit timestamp marking real-time data adjustments.
    /// </summary>
    public DateTime? LastModifiedAt { get; protected set; }

    /// <summary>
    /// 🔒 SECURITY ABAC ATTRIBUTE: Captures the identity of the last operator (Retail User or Admin Override agent).
    /// </summary>
    public string? LastModifiedByEmail { get; protected set; }

    /// <summary>
    /// Thread-insulated domain event collection wrapper dispatched exclusively upon successful unit-of-work state commits.
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Appends a domain action signal to the in-memory event infrastructure dispatch chain.
    /// </summary>
    public void AddDomainEvent(object domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears out the transaction tracking queue hooks once domain event handlers complete execution loops successfully.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Sets initial creation tracking attributes. Invoked natively by the Application DB Context interception layer.
    /// </summary>
    public void SetCreationAuditTelemetry(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Audit telemetry trace requires a valid user identity email context.", nameof(operatorEmail));

        CreatedByEmail = operatorEmail.ToLowerInvariant();
    }

    /// <summary>
    /// Enforces explicit ledger modifications auditing tracks cleanly inside state mutator operations.
    /// </summary>
    public void MarkAsModified(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        LastModifiedAt = DateTime.UtcNow;
        LastModifiedByEmail = operatorEmail.ToLowerInvariant();
    }

    /// <summary>
    /// Domain Object Identity Evaluation Override.
    /// Evaluates structural equality using database key values rather than unstable, volatile transient memory references.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        // IsTransient Check: If either entity possesses an empty tracking key, fallback cleanly to absolute object references
        if (Id == Guid.Empty || other.Id == Guid.Empty) return ReferenceEquals(this, other);

        return Id == other.Id;
    }

    /// <summary>
    /// Returns a deterministic identity hashcode mapped directly to the unique tracking Guid value.
    /// </summary>
    public override int GetHashCode()
    {
        // Incorporates structural runtime types into the cryptographic footprint to prevent domain identity crossing collisions
        return HashCode.Combine(Id, GetType());
    }

    // =============================================================================
    // 🧱 STRICT ARCHITECTURAL EQUALITY OPERATOR OVERRIDES
    // =============================================================================
    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}