using System;

namespace Domain.Enums;

/// <summary>
/// Defines the authorized classification vectors for portfolio allocation assets [CL-201 / CL-203 / CL-206].
/// Governs structural validation behavior, caching loops, and dynamic math equations across the platform.
/// </summary>
public enum AssetCategory
{
    /// <summary>
    /// Pakistan Stock Exchange (PSX) Blue-chip equities tracked via high-frequency cached scrapers.
    /// </summary>
    PsxEquity = 1,

    /// <summary>
    /// Digital assets and tokens calculated via live Binance Spot/Earn API connection channels.
    /// </summary>
    Crypto = 2,

    /// <summary>
    /// Asset Management Company positions tracked via mutual fund Net Asset Value (NAV) feed lines (e.g., Al Meezan).
    /// </summary>
    MutualFund = 3,

    /// <summary>
    /// Foreign Currency bank ledger accounts normalized to local values via real-time exchange rates (e.g., Al Habib FX).
    /// </summary>
    FcyBank = 4,

    /// <summary>
    /// Physical spot commodities measured strictly in corporate Tolas (Gold and Silver bullion tracking structures).
    /// </summary>
    PreciousMetal = 5,

    /// <summary>
    /// Real Estate property allocation assets, housing developments, plots, or installments tracking schemas.
    /// </summary>
    RealEstate = 6,

    /// <summary>
    /// 🛠️ ALIGNED MATRIX COMPONENT: Traditional managed stock broker accounts.
    /// Natively utilized by TriggerApiSyncCommandHandler to invoke IAkdBrokerageSyncService handshakes.
    /// </summary>
    Brokerage = 7
}