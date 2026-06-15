namespace Domain.Enums;

/// <summary>
/// Defines the regulatory risk profiling categories mandated under SECP and SBP 
/// compliance frameworks for digital robo-advisory asset platforms.
/// </summary>
public enum RiskToleranceMatrix
{
    /// <summary>
    /// Initial onboarding baseline state prior to completing the suitability assessment wizard.
    /// </summary>
    Unassigned = 0,

    /// <summary>
    /// Defensive allocations. Heavily locks asset distribution vectors toward low-volatility 
    /// tools, including Capital Protection or Shariah-Compliant Income Mutual Funds.
    /// </summary>
    Conservative = 1,

    /// <summary>
    /// Standard allocations model. Intercepts routing engines to authorize medium exposure 
    /// capabilities across traditional PSX equity boards and blue-chip stock ledgers.
    /// </summary>
    Balanced = 2,

    /// <summary>
    /// Aggressive expansion trajectory profiles. Minimizes system-enforced safety caps, 
    /// authorizing advanced high-yield exposure parameters (e.g., automated digital wallets and crypto nodes sync).
    /// </summary>
    Growth = 3
}