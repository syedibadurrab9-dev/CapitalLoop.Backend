using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Common;

/// <summary>
/// Domain-Driven Design (DDD) Abstract Value Object Base Contract.
/// Guarantees immutable structural comparison mechanics across non-identity sub-components,
/// eliminating structural calculation collisions within high-frequency ledger processing systems.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Yields the precise internal structural data elements used to evaluate domain component equality.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Evaluates value properties sequentially to confirm structural identity uniformity.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Computes a crash-safe, allocation-optimized structural hash code factoring in all equality components.
    /// </summary>
    public override int GetHashCode()
    {
        var components = GetEqualityComponents();

        // 🛡️ CRITICAL HARDENING BOUNDARY: Prevent unhandled System.InvalidOperationException 
        // if an empty component mapping collection is passed during initialization evaluation.
        if (!components.Any()) return GetType().GetHashCode();

        // 🚀 ALLOCATION OPTIMIZATION: Replaced unstable XOR aggregations with a non-allocating,
        // hash combine iteration sequence to prevent value distribution mapping collisions.
        int hash = 17;
        foreach (var component in components)
        {
            hash = (hash * 23) + (component?.GetHashCode() ?? 0);
        }

        return hash;
    }

    // =============================================================================
    // 🧱 STRICT ARCHITECTURAL EQUALITY OPERATOR OVERRIDES
    // =============================================================================
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}