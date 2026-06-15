using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Domain Entity Governing Interbank Foreign Exchange Translation Rates.
/// Caches global tracking lookup metrics natively optimized to preserve high-precision decimal variables.
/// </summary>
public class ExchangeRate : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string FromCurrency { get; private set; } = string.Empty;
    public string ToCurrency { get; private set; } = string.Empty;

    // Natively mapped in EF Configurations to achieve strict decimal(18,6) precision
    public decimal Rate { get; private set; }
    public DateTime LastUpdatedUtc { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private ExchangeRate() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Instantiates a type-safe currency translation mapping record.
    /// </summary>
    /// <param name="fromCurrency">The ISO source currency token string (e.g., "USD").</param>
    /// <param name="toCurrency">The ISO target currency token string (e.g., "PKR").</param>
    /// <param name="rate">The baseline numerical conversion weight metric.</param>
    /// <param name="operatorEmail">The security principal context execution identity initiating this currency mapping.</param>
    public static ExchangeRate Create(string fromCurrency, string toCurrency, decimal rate, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency)) throw new ArgumentException("Source ISO currency code is required.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency)) throw new ArgumentException("Target ISO currency code is required.", nameof(toCurrency));
        if (rate <= 0) throw new ArgumentException("Exchange rate tracking arguments must represent a positive non-zero value.", nameof(rate));
        if (string.IsNullOrWhiteSpace(operatorEmail)) throw new ArgumentException("Audit telemetry trace requires a valid user identity email context.", nameof(operatorEmail));

        var exchangeRate = new ExchangeRate
        {
            Id = Guid.NewGuid(),
            FromCurrency = fromCurrency.Trim().ToUpperInvariant(),
            ToCurrency = toCurrency.Trim().ToUpperInvariant(),
            Rate = rate,
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Automate database validation audit footprints natively upon instance generation
        exchangeRate.SetCreationAuditTelemetry(operatorEmail);

        return exchangeRate;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF STATE MUTATORS & INVARIANT VALIDATORS
    // =============================================================================

    /// <summary>
    /// Updates the conversion value factor directly based on verified interbank financial rate feeds.
    /// </summary>
    /// <param name="newRate">The verified non-zero positive conversion multiplier index.</param>
    /// <param name="operatorEmail">The security principal context execution identity executing this rate update.</param>
    public void UpdateRate(decimal newRate, string operatorEmail)
    {
        if (newRate <= 0) throw new ArgumentException("Updated exchange rate must be a positive non-zero value.", nameof(newRate));
        if (string.IsNullOrWhiteSpace(operatorEmail)) throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        // Idempotency protection check to prevent unnecessary transaction dirtying
        if (Rate == newRate) return;

        Rate = newRate;
        LastUpdatedUtc = DateTime.UtcNow;

        // Push explicit security claims markers down into the entity audit trail tracking layer
        MarkAsModified(operatorEmail);
    }
}