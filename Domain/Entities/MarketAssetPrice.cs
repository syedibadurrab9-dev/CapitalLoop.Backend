using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// 📈 PLATFORM SPOT VALUATION LOG RECORD
/// Captures high-frequency ticker values and tracking metrics across equities, commodities, and digital units.
/// Feeds your background rebalancing engine and provides the baseline valuation lookup table for portfolio assets.
/// </summary>
public class MarketAssetPrice : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string AssetTicker { get; private set; } = string.Empty;
    public string AssetName { get; private set; } = string.Empty;
    public string AssetClass { get; private set; } = string.Empty;
    public decimal UnitPricePkr { get; private set; }
    public DateTime LastSyncedUtc { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private MarketAssetPrice() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Factory pattern to instantiate a pristine, tracked market price node using base PKR parameters.
    /// </summary>
    /// <param name="ticker">The asset trading ticker handle (e.g., "ENGRO", "BTC", "GOLD").</param>
    /// <param name="name">The public descriptive title or corporate moniker of the position.</param>
    /// <param name="assetClass">The categorization family (e.g., PSX Equity, Crypto, Precious Metal).</param>
    /// <param name="unitPricePkr">The current relative value of a single unit denominated in PKR.</param>
    /// <param name="operatorEmail">The identity of the process context initializing this asset value tracking record.</param>
    public static MarketAssetPrice Create(string ticker, string name, string assetClass, decimal unitPricePkr, string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Asset ticker reference is required.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset moniker description name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(assetClass))
            throw new ArgumentException("Asset functional class categorization is required.", nameof(assetClass));
        if (unitPricePkr < 0)
            throw new ArgumentException("Unit price parameters cannot scale below zero.", nameof(unitPricePkr));
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Audit telemetry trace requires a valid user identity email context.", nameof(operatorEmail));

        var assetPrice = new MarketAssetPrice
        {
            Id = Guid.NewGuid(),
            AssetTicker = ticker.Trim().ToUpperInvariant(),
            AssetName = name.Trim(),
            AssetClass = assetClass.Trim(),
            UnitPricePkr = unitPricePkr,
            LastSyncedUtc = DateTime.UtcNow
        };

        // Seal the absolute data verification trail during initialization inside the persistence block
        assetPrice.SetCreationAuditTelemetry(operatorEmail);

        return assetPrice;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF STATE MUTATORS & INVARIANT VALIDATORS
    // =============================================================================

    /// <summary>
    /// Refreshes the baseline PKR pricing matrix following a successful local market sweep.
    /// Used directly by your localized web scraping or API synchronization engines.
    /// </summary>
    /// <param name="identityPricePkr">The newly calculated spot price tracking in PKR.</param>
    /// <param name="operatorEmail">The identity execution context string of the automation worker driving the state update.</param>
    public void RefreshPrice(decimal identityPricePkr, string operatorEmail)
    {
        if (identityPricePkr < 0)
            throw new ArgumentException("Refreshed pricing matrix values cannot drop below zero.", nameof(identityPricePkr));
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        // Idempotency check to clear downstream pipeline processing dirty writes
        if (UnitPricePkr == identityPricePkr) return;

        UnitPricePkr = identityPricePkr;
        LastSyncedUtc = DateTime.UtcNow;

        // Propagate the background agent context directly to the base audit tracker properties
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Refreshes the spot record from a foreign currency token by applying a live desk conversion rate inline.
    /// Universally isolates foreign valuation metrics like global crypto or spot metal counts into domestic PKR tokens.
    /// </summary>
    /// <param name="foreignPriceUsd">The asset valuation denominated in foreign currency (USD).</param>
    /// <param name="liveUsdPkrRate">The active currency conversion rate multiplier from the market service.</param>
    /// <param name="operatorEmail">The system background service identity driving the translation execution block.</param>
    public void RefreshForeignPrice(decimal foreignPriceUsd, decimal liveUsdPkrRate, string operatorEmail)
    {
        if (foreignPriceUsd < 0)
            throw new ArgumentException("Foreign asset valuation parameters cannot scale below zero.", nameof(foreignPriceUsd));
        if (liveUsdPkrRate <= 0)
            throw new ArgumentException("Currency conversion desk rates must settle above zero bounds.", nameof(liveUsdPkrRate));
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("Modification trace requires a valid user identity email context.", nameof(operatorEmail));

        decimal localizedPkrValue = foreignPriceUsd * liveUsdPkrRate;
        decimal roundedValue = Math.Round(localizedPkrValue, 2);

        // Idempotency block shortcut
        if (UnitPricePkr == roundedValue) return;

        UnitPricePkr = roundedValue;
        LastSyncedUtc = DateTime.UtcNow;

        // Propagate the background agent context directly to the base audit tracker properties
        MarkAsModified(operatorEmail);
    }
}