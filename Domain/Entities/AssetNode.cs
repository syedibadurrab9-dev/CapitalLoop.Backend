using System;
using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

/// <summary>
/// Domain Entity Governing an Active Asset Allocation Position [CL-201 / CL-203 / CL-206].
/// Tracks and stores multi-asset parameters like precious metals, real estate files, fiat cash, 
/// and digital tokens, managing automated API sync nodes and manual ledger records.
/// </summary>
public class AssetNode : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================
    public string UserEmail { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AssetCategory Category { get; private set; }

    // PROPERTY SYNCHRONIZATION MATRIX: Updated to unify with your master CQRS query projections
    public decimal ValuationPkr { get; private set; }
    public bool IsAutoSynced { get; private set; }

    // EXTRA FIELDS REQUIRED BY TRIGGERAPISYNCCOMMAND: Maps asset quantities and ticker variations natively
    public string? Symbol { get; private set; } // E.g., "SYS", "GOLD", "ENGRO", "BTC"
    public decimal QuantityHoldings { get; private set; } // Tracks dynamic units owned (shares count, tolas weight, crypto volume)

    // Real estate installment metadata parameters
    public int? InstallmentsPaid { get; private set; }
    public int? TotalInstallments { get; private set; }

    // Traditional precious metals structural fields (Gold / Silver tracking)
    // 🧠 RE-ALIGNED READ-ONLY COMPUTED PROPERTY: This will be skipped by EF columns natively
    public decimal? TotalTolas => Category == AssetCategory.PreciousMetal ? QuantityHoldings : null;

    // 🚀 FIXED: Added private set configuration so the EF Core hydrator engine can populate this table field natively
    public decimal? CostPerTola { get; private set; }

    // Automated API keys reference boundary mapping
    public string? EncryptedApiKey { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private AssetNode() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Factory for typical manual ledger positions (e.g. Stocks, Mutual Funds, Real Estate files).
    /// </summary>
    public static AssetNode CreateManual(string email, string name, AssetCategory category, decimal value, int? paid = null, int? total = null, string? symbol = null, decimal quantity = 0)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Asset validation failed: A valid user email address contract structure must be provided.", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset validation failed: Asset designation moniker references cannot match empty sequences.", nameof(name));

        if (value < 0)
            throw new InvalidAssetValueException("Asset valuation parameters must represent positive numeric sums.");

        // 🛡️ REAL ESTATE COMPLIANCE GATEWAY
        if (category == AssetCategory.RealEstate)
        {
            if (paid.HasValue && total.HasValue && paid.Value > total.Value)
                throw new InvalidInstallmentSchemaException("Completed payments cannot outpace your master real estate file framework.");

            if ((paid.HasValue && paid.Value < 0) || (total.HasValue && total.Value <= 0))
                throw new InvalidInstallmentSchemaException("Installment numeric allocations must represent realistic positive metrics.");
        }

        var node = new AssetNode
        {
            Id = Guid.NewGuid(),
            UserEmail = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Category = category,
            ValuationPkr = value,
            IsAutoSynced = false,
            Symbol = symbol?.Trim().ToUpperInvariant(),
            QuantityHoldings = quantity,
            InstallmentsPaid = category == AssetCategory.RealEstate ? paid : null,
            TotalInstallments = category == AssetCategory.RealEstate ? total : null
        };

        // Automate data validation audit footprints at inception using current owner address parameters
        node.SetCreationAuditTelemetry(node.UserEmail);

        return node;
    }

    /// <summary>
    /// Specialized factory for processing physical precious metal investments calculated in structural Tolas.
    /// </summary>
    public static AssetNode CreateCommodityTola(string email, string name, decimal tolas, decimal costPerTola, string? symbol = "GOLD")
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Commodity validation failed: A valid user email address contract structure must be provided.", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Commodity validation failed: Commodity designation name references cannot match empty sequences.", nameof(name));

        if (tolas <= 0 || costPerTola <= 0)
            throw new InvalidAssetValueException("Custodial physical commodity weight parameters or purchase rules cannot equal zero or be negative.");

        // 🚨 ECOSYSTEM PROTECTION CAP: Safeguard physical tracking structures from extreme data anomalies
        if (tolas > 500)
            throw new AssetCapExceededException("Asset processing request weight limits exceed baseline physical custody security thresholds.");

        var node = new AssetNode
        {
            Id = Guid.NewGuid(),
            UserEmail = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            Category = AssetCategory.PreciousMetal,
            QuantityHoldings = tolas,
            CostPerTola = costPerTola,
            Symbol = symbol?.Trim().ToUpperInvariant() ?? "GOLD",
            ValuationPkr = tolas * costPerTola, // Tracks historical acquisition cost baseline natively
            IsAutoSynced = false
        };

        // Automate data validation audit footprints at inception using current owner address parameters
        node.SetCreationAuditTelemetry(node.UserEmail);

        return node;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF MUTATORS & SYSTEM BALANCE SYNCERS
    // =============================================================================

    /// <summary>
    /// Synchronizes external CEX balances or asset units via secure automation loops, mapping unique key constraints.
    /// </summary>
    /// <param name="synchedValue">The freshly synchronized position balance calculated down from external provider loops.</param>
    /// <param name="encryptedApiKey">The AES-encrypted credential reference hash linked to this specific node pipeline.</param>
    /// <param name="operatorEmail">The security principal context execution identity initiating the current synchronization context.</param>
    public void HydrateLiveBalanceFromApi(decimal synchedValue, string encryptedApiKey, string operatorEmail)
    {
        if (synchedValue < 0)
            throw new InvalidAssetValueException("Synchronized API asset valuation parameters must represent positive numeric sums.");

        if (string.IsNullOrWhiteSpace(encryptedApiKey))
            throw new ArgumentException("Automated sync targets must supply valid secure tracking API identifier hashes.", nameof(encryptedApiKey));

        ValuationPkr = synchedValue;
        EncryptedApiKey = encryptedApiKey.Trim();
        IsAutoSynced = true;

        MarkAsModified(operatorEmail); // Updates base framework timestamp audit tracks cleanly via the security context
    }

    /// <summary>
    /// Updates the local valuation field directly for spot physical commodities based on real-time market rate streams.
    /// </summary>
    /// <param name="currentSpotRatePerTola">The verified real-time market spot price fetched from precious metals scraper nodes.</param>
    /// <param name="operatorEmail">The security principal context execution identity initiating the current spot rate recalculation.</param>
    public void RecalculateCommodityMarketValue(decimal currentSpotRatePerTola, string operatorEmail)
    {
        if (Category != AssetCategory.PreciousMetal)
            throw new InvalidOperationException("Market value re-evaluation rejected: Active asset node context is not initialized as a precious metal commodity.");

        if (currentSpotRatePerTola <= 0)
            throw new InvalidAssetValueException("Market spot valuation recalculation requires a positive non-zero spot rate asset argument.");

        ValuationPkr = QuantityHoldings * currentSpotRatePerTola;

        MarkAsModified(operatorEmail); // Commits contextual operator footprints straight to the baseline properties
    }
}