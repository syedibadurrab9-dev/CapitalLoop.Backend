namespace Application.Common.Interfaces;

public interface IMarketDataService
{
    /// <summary>
    /// Fetches the live spot market price per Tola for physical Gold and Silver bullion.
    /// Used by PreciousMetalsClient to recalculate manual user commodities inventory.
    /// </summary>
    Task<(decimal GoldPricePerTola, decimal SilverPricePerTola)> GetPreciousMetalsSpotPricesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the live spot market exchange price for USD to PKR conversion metrics.
    /// Crucial for normalizing digital assets (Binance Spot/Futures/Earn) into localized PKR.
    /// </summary>
    Task<decimal> GetLiveUsdPkrRateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches closing or current price metrics for a given equity symbol listed on the Pakistan Stock Exchange.
    /// Future-proofed to accept custom tickers (e.g., "ENGRO", "SYS") via AKD or K-Trade scrapers.
    /// </summary>
    Task<decimal> GetPsxStockPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls the current Net Asset Value (NAV) pricing index per unit for specialized open-ended funds.
    /// Dedicated for syncing Islamic or conventional mutual fund units (e.g., Al Meezan Investments).
    /// </summary>
    Task<decimal> GetMutualFundNavAsync(string fundCode, CancellationToken cancellationToken = default);
}