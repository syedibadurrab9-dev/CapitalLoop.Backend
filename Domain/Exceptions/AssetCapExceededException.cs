using System;

namespace Domain.Exceptions;

/// <summary>
/// Domain Exception Thrown when an asset validation or ingestion request violates safe portfolio threshold caps 
/// or physical custodial custody constraints established within the domain aggregate roots [CL-203].
/// </summary>
public class AssetCapExceededException : Exception
{
    // =============================================================================
    // 🧱 SECURE CONTEXTUAL TELEMETRY METADATA (IMMUTABLE PROPERTY CELLS)
    // =============================================================================

    /// <summary>
    /// Gets the target asset classification code string that triggered the rule violation filter.
    /// </summary>
    public string? AssetCategoryContext { get; private set; }

    /// <summary>
    /// Gets the high-frequency value or commodity unit volume attempted by the request execution frame.
    /// </summary>
    public decimal AttemptedQuantity { get; private set; }

    /// <summary>
    /// Gets the maximum system capability or maximum physical threshold bounds configured for this asset asset class node.
    /// </summary>
    public decimal DefinedSystemCap { get; private set; }

    // =============================================================================
    // 🚀 HARDENED EXCEPTION FACTORY OVERRIDES
    // =============================================================================

    public AssetCapExceededException()
        : base("The requested asset transaction parameters exceed safe system threshold boundaries or verification limits.") { }

    public AssetCapExceededException(string message)
        : base(message) { }

    public AssetCapExceededException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Master Enterprise Factory Constructor.
    /// Captures detailed calculation metrics to drive structured JSON responses within global error interceptors.
    /// </summary>
    /// <param name="message">The specific localized policy violation text description.</param>
    /// <param name="category">The string representation of the target asset classification category vector.</param>
    /// <param name="attempted">The violating volume or value summation attempted by the user context.</param>
    /// <param name="maxCap">The structural maximum constraint allowed under current platform validation configurations.</param>
    public AssetCapExceededException(string message, string category, decimal attempted, decimal maxCap)
        : base(message)
    {
        AssetCategoryContext = category;
        AttemptedQuantity = attempted;
        DefinedSystemCap = maxCap;
    }
}