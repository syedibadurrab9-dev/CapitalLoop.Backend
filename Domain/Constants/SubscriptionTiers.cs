using System;
using System.Collections.Generic;

namespace Domain.Constants;

/// <summary>
/// 🛡️ CENTRALIZED PLATFORM SUBSCRIPTION MATRIX CONTRACT [CL-503]
/// Defines immutable system invariants, authorization profiles, feature gate tokens, 
/// and cross-interface runtime accessibility profiles across the CapitalLoop ecosystem.
/// </summary>
public static class SubscriptionTiers
{
    // =============================================================================
    // 📱 RETAIL WORKSPACE SYSTEM ROLES (MOBILE CLIENT CHANNELS)
    // =============================================================================
    public const string StandardFree = "standard-free";
    public const string Premium = "premium"; // Mapped from premium-monthly to align with database models [CL-503]

    // =============================================================================
    // 🏢 ENTERPRISE MANAGEMENT WORKSPACE SYSTEM ROLES (ANGULAR WEB CONSOLE CHANNELS)
    // =============================================================================
    public const string AgentBackoffice = "agent-backoffice";
    public const string AdvisorWealth = "advisor-wealth";
    public const string TaxConsultantTier = "tax-consultant-tier";

    // =============================================================================
    // 🎛️ SYSTEM PERMISSION FEATURE TOKENS (SUBSCRIPTION REQUIREMENT POLICY ENGINE)
    // =============================================================================
    public const string ManualLedgerTracking = "Manual_Ledger_Tracking";
    public const string AutomatedExchangeSync = "Automated_Exchange_Sync";
    public const string AdvancedTaxScraping = "Advanced_Tax_Scraping";
    public const string DedicatedLiveChat = "Dedicated_Live_Chat";
    public const string StrategicRoboInsights = "Strategic_Robo_Insights";

    // 🚀 BACKWARDS-COMPATIBILITY COMPILATION ANCHORS
    public const string BasePortfolioTracking = ManualLedgerTracking;

    private static readonly HashSet<string> ValidTierRegistry = new(StringComparer.OrdinalIgnoreCase)
    {
        StandardFree,
        Premium,
        AgentBackoffice,
        AdvisorWealth,
        TaxConsultantTier
    };

    // Maps granular application features explicitly to specialized membership limits
    private static readonly Dictionary<string, HashSet<string>> FeatureMatrix = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            StandardFree, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManualLedgerTracking
            }
        },
        {
            Premium, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManualLedgerTracking, AutomatedExchangeSync, DedicatedLiveChat, StrategicRoboInsights
            }
        },
        {
            AgentBackoffice, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManualLedgerTracking, AutomatedExchangeSync, DedicatedLiveChat
            }
        },
        {
            AdvisorWealth, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManualLedgerTracking, AutomatedExchangeSync, StrategicRoboInsights
            }
        },
        {
            TaxConsultantTier, new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManualLedgerTracking, AdvancedTaxScraping
            }
        }
    };

    /// <summary>
    /// 🛡️ DOMAIN INVARIANT RULE EVALUATOR
    /// Verifies if an incoming plan parameter matches one of the five authorized system roles.
    /// </summary>
    public static bool IsValidTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
            return false;

        // Zero-allocation fallback check leveraging our ordinal set register
        return ValidTierRegistry.Contains(tier);
    }

    /// <summary>
    /// 📱 PLATFORM PRIVILEGE BOUNDARY VERIFIER
    /// Evaluates if an authorized identity classification is cleared to access the Mobile Client Workspace.
    /// </summary>
    public static bool IsMobileAllowed(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
            return false;

        return tier.Equals(StandardFree, StringComparison.OrdinalIgnoreCase) ||
               tier.Equals(Premium, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 🛡️ CENTRALIZED FEATURE GATEKEEPER MECHANIC
    /// Evaluates whether a given subscription tier is authorized to execute a specific application feature.
    /// This completely decouples core feature gates from hardcoded controller layers.
    /// </summary>
    public static bool IsFeatureAuthorized(string? tier, string featureToken)
    {
        if (string.IsNullOrWhiteSpace(tier) || string.IsNullOrWhiteSpace(featureToken))
            return false;

        if (FeatureMatrix.TryGetValue(tier, out var assignedFeatures))
        {
            return assignedFeatures.Contains(featureToken);
        }

        return false;
    }
}