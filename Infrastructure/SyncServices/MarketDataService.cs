using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.SyncServices;

public class MarketDataService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<MarketDataService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    #region 1. FIAT CURRENCY CONVERSION (USD -> PKR)

    /// <summary>
    /// Fetches the live fiat currency conversion rate utilizing open exchange rate aggregates.
    /// </summary>
    public async Task<decimal> GetLiveUsdPkrRateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Leverages standard provider interfaces (e.g., ExchangeRate-API or OpenExchangeRates)
            var apiKey = _config["ExchangeRateApi:Key"];
            var url = $"https://v6.exchangerate-api.com/v6/{apiKey}/latest/USD";

            var response = await _httpClient.GetFromJsonAsync<ExchangeRateApiResponse>(url, cancellationToken);

            if (response?.ConversionRates != null && response.ConversionRates.TryGetValue("PKR", out var rate))
            {
                _logger.LogInformation("[MARKET DATA] Successfully retrieved USD/PKR rate: {Rate}", rate);
                return rate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MARKET DATA] Failed retrieving USD/PKR rate. Falling back to platform default.");
        }

        return 278.50m; // Resilient system fallback rate baseline
    }

    #endregion

    #region 2. COMMODITIES NODE (GOLD & SILVER SPOT METRICS)

    /// <summary>
    /// Maps global commodity ounces indices and converts them cleanly into local Pakistani Tola metrics.
    /// </summary>
    public async Task<(decimal GoldPricePerTola, decimal SilverPricePerTola)> GetPreciousMetalsSpotPricesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var goldApiKey = _config["GoldApi:Key"]; // e.g., GoldAPI.io configuration blocks
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-access-token", goldApiKey);

            // Fetch live price per ounce in USD for Gold (XAU) and Silver (XAG)
            var goldTask = _httpClient.GetFromJsonAsync<GoldApiPriceResponse>("https://www.goldapi.io/api/XAU/USD", cancellationToken);
            var silverTask = _httpClient.GetFromJsonAsync<GoldApiPriceResponse>("https://www.goldapi.io/api/XAG/USD", cancellationToken);

            await Task.WhenAll(goldTask, silverTask);
            var usdPkrRate = await GetLiveUsdPkrRateAsync(cancellationToken);

            // Conversion Math: 1 Troy Ounce = 31.1034768 Grams. 1 Local Pakistani Tola = 11.6638125 Grams.
            // Ounce to Tola Multiplier Constant = 11.6638125 / 31.1034768 ≈ 0.375
            const decimal ounceToTolaConstant = 0.375m;

            decimal goldTolaPkr = (goldTask.Result?.Price ?? 0m) * ounceToTolaConstant * usdPkrRate;
            decimal silverTolaPkr = (silverTask.Result?.Price ?? 0m) * ounceToTolaConstant * usdPkrRate;

            _logger.LogInformation("[METALS] Calculated Rates per Tola -> Gold: {Gold} PKR | Silver: {Silver} PKR", goldTolaPkr, silverTolaPkr);
            return (goldTolaPkr, silverTolaPkr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[METALS DATA] Failed parsing precious metals prices. Returning baseline fallbacks.");
            return (220000.00m, 2600.00m); // Operational localized safety fallback positions
        }
    }

    #endregion

    #region 3. PAKISTAN STOCK EXCHANGE (PSX - AKD / K-TRADE MODULE)

    /// <summary>
    /// Future-Proof Scraper Node: Extracts individual ticker closing arrays straight from the PSX Data Portal.
    /// </summary>
    public async Task<decimal> GetPsxStockPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            // Scrapes closing metrics straight from the high-performance official portal data sheets
            var targetUrl = $"https://dps.psx.com.pk/company/{symbol.Trim().ToUpper()}";
            var htmlContent = await _httpClient.GetStringAsync(targetUrl, cancellationToken);

            // Regular Expression mapping out target data node markers inside the PSX theme source: <div class="stats_value">123.45</div>
            var regexPattern = new Regex(@"<div class=""stats_value"">([\d\.]+)<\/div>", RegexOptions.IgnoreCase);
            var matches = regexPattern.Matches(htmlContent);

            if (matches.Count > 0 && decimal.TryParse(matches[0].Groups[1].Value, out var extractedStockPrice))
            {
                _logger.LogInformation("[PSX] Successfully scraped live asset valuation for {Symbol}: {Price} PKR", symbol, extractedStockPrice);
                return extractedStockPrice;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PSX SCRAPER] Failed pulling live equity metrics for symbol: {Symbol}", symbol);
        }

        return 0m; // Fallback handles structural calculations cleanly without throwing execution loops
    }

    #endregion

    #region 4. MUTUAL FUNDS CONTAINER (AL MEEZAN NAV EXTENSIONS)

    /// <summary>
    /// Future-Proof Portal Interface: Tracks net values per unit emitted by regional asset management boards.
    /// </summary>
    public async Task<decimal> GetMutualFundNavAsync(string fundCode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MUTUAL FUNDS] Initializing background vector lookup for Fund Registry: {Code}", fundCode);

        // TODO: Target specialized fund parsing endpoints (e.g., Al Meezan's daily corporate published tables)
        // For development scaffolding, simulate a valid tracking valuation return block:
        await Task.CompletedTask;
        return fundCode.ToUpper() == "MIF" ? 64.50m : 100.00m;
    }

    #endregion

    #region INTERNAL API MAPPING SCHEMAS

    private class ExchangeRateApiResponse
    {
        public System.Collections.Generic.Dictionary<string, decimal>? ConversionRates { get; set; }
    }

    private class GoldApiPriceResponse
    {
        public decimal Price { get; set; } // Returns asset baseline spot valuation per Troy Ounce
    }

    #endregion
}