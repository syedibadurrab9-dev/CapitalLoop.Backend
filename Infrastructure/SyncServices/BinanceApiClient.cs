using Application.Common.DTOs;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.SyncServices;

public class BinanceApiClient : ICryptoSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<BinanceApiClient> _logger;
    private readonly IMarketDataService _marketDataService;

    public BinanceApiClient(
        HttpClient httpClient,
        ResiliencePipeline resiliencePipeline,
        ILogger<BinanceApiClient> logger,
        IMarketDataService marketDataService)
    {
        _httpClient = httpClient;
        _resiliencePipeline = resiliencePipeline;
        _logger = logger;
        _marketDataService = marketDataService;
    }

    #region 1. MASTER COHESIVE SWEEP (COMPREHENSIVE AGGREGATION)

    public async Task<BinanceSyncResult> FetchExtendedPortfolioBalancesAsync(string apiKey, string secretKey)
    {
        _logger.LogInformation("[SYNC] Initiating multi-wallet cryptographic snapshot sweep (Spot + Futures + Earn).");

        var pkrUsdRate = await _marketDataService.GetLiveUsdPkrRateAsync();
        var resilienceContext = ResilienceContextPool.Shared.Get();

        try
        {
            return await _resiliencePipeline.ExecuteAsync(async context =>
            {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var queryString = $"timestamp={timestamp}&recvWindow=60000";
                var signature = ComputeHmacSha256(queryString, secretKey);

                // Fetch Prices Map Cache for Valuation Layers
                var pricesMap = await GetPricesMapAsync(context.CancellationToken);

                // Execute concurrent API calls for all target balance wallets
                var spotTask = FetchSpotRawJsonAsync(apiKey, queryString, signature, context.CancellationToken);
                var futuresTask = FetchFuturesRawJsonAsync(apiKey, queryString, secretKey, context.CancellationToken);
                var earnTask = FetchEarnRawJsonAsync(apiKey, queryString, signature, context.CancellationToken);

                await Task.WhenAll(spotTask, futuresTask, earnTask);

                decimal spotUsdValue = ParseSpotValue(spotTask.Result, pricesMap);
                decimal futuresUsdValue = ParseFuturesValue(futuresTask.Result);
                decimal earnUsdValue = ParseEarnValue(earnTask.Result, pricesMap);

                return new BinanceSyncResult
                {
                    SpotBalancePkr = spotUsdValue * pkrUsdRate,
                    EarnBalancePkr = earnUsdValue * pkrUsdRate,
                    FuturesBalancePkr = futuresUsdValue * pkrUsdRate
                };

            }, resilienceContext);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }
    }

    #endregion

    #region 2. GRANULAR INTERFACE EXTRACTIONS (NEW INDEPENDENT MODULES)

    public async Task<decimal> GetSpotWalletBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SYNC] Executing isolated high-frequency Spot Wallet tracking query.");

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryString = $"timestamp={timestamp}&recvWindow=60000";
        var signature = ComputeHmacSha256(queryString, secretKey);

        var pricesMap = await GetPricesMapAsync(cancellationToken);
        var rawJson = await FetchSpotRawJsonAsync(apiKey, queryString, signature, cancellationToken);

        return ParseSpotValue(rawJson, pricesMap);
    }

    public async Task<decimal> GetFuturesWalletBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SYNC] Auditing active leveraged risk balances via isolated Futures API node.");

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryString = $"timestamp={timestamp}&recvWindow=60000";

        var rawJson = await FetchFuturesRawJsonAsync(apiKey, queryString, secretKey, cancellationToken);

        return ParseFuturesValue(rawJson);
    }

    public async Task<decimal> GetSimpleEarnBalanceInUsdAsync(string apiKey, string secretKey, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[SYNC] Extracting capital deployment positions from flexible Simple Earn staging tracks.");

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryString = $"timestamp={timestamp}&recvWindow=60000";
        var signature = ComputeHmacSha256(queryString, secretKey);

        var pricesMap = await GetPricesMapAsync(cancellationToken);
        var rawJson = await FetchEarnRawJsonAsync(apiKey, queryString, signature, cancellationToken);

        return ParseEarnValue(rawJson, pricesMap);
    }

    #endregion

    #region SECURE ENDPOINT CONCURRENT CALLERS

    private async Task<string> FetchSpotRawJsonAsync(string apiKey, string query, string sig, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.binance.com/api/v3/account?{query}&signature={sig}");
        request.Headers.Add("X-MBX-APIKEY", apiKey);
        var res = await _httpClient.SendAsync(request, token);
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync(token) : string.Empty;
    }

    private async Task<string> FetchFuturesRawJsonAsync(string apiKey, string query, string secretKey, CancellationToken token)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string futuresQuery = $"timestamp={timestamp}&recvWindow=60000";
        string futuresSig = ComputeHmacSha256(futuresQuery, secretKey);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://fapi.binance.com/fapi/v2/account?{futuresQuery}&signature={futuresSig}");
        request.Headers.Add("X-MBX-APIKEY", apiKey);
        var res = await _httpClient.SendAsync(request, token);
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync(token) : string.Empty;
    }

    private async Task<string> FetchEarnRawJsonAsync(string apiKey, string query, string sig, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.binance.com/sapi/v1/simple-earn/flexible/position?{query}&signature={sig}");
        request.Headers.Add("X-MBX-APIKEY", apiKey);
        var res = await _httpClient.SendAsync(request, token);
        return res.IsSuccessStatusCode ? await res.Content.ReadAsStringAsync(token) : string.Empty;
    }

    #endregion

    #region DATA EXTRACTION & PRICING UTILITIES

    private async Task<Dictionary<string, decimal>> GetPricesMapAsync(CancellationToken token)
    {
        var tickerResponse = await _httpClient.GetAsync("https://api.binance.com/api/v3/ticker/price", token);
        tickerResponse.EnsureSuccessStatusCode();
        var tickers = await tickerResponse.Content.ReadFromJsonAsync<List<BinanceTickerPrice>>(cancellationToken: token)
                      ?? new List<BinanceTickerPrice>();

        return tickers
            .Where(t => !string.IsNullOrWhiteSpace(t.Symbol))
            .ToDictionary(t => t.Symbol!, t => decimal.TryParse(t.Price, out var p) ? p : 0m);
    }

    private decimal ParseSpotValue(string json, Dictionary<string, decimal> pricesMap)
    {
        if (string.IsNullOrEmpty(json)) return 0m;
        var data = JsonSerializer.Deserialize<BinanceAccountResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        decimal totalUsd = 0m;
        if (data?.Balances == null) return 0m;

        foreach (var balance in data.Balances)
        {
            if (decimal.TryParse(balance.Free, out var volume) && volume > 0)
            {
                var asset = balance.Asset!.StartsWith("LD") && balance.Asset.Length > 2 ? balance.Asset.Substring(2) : balance.Asset;
                totalUsd += volume * GetAssetPriceInUsdt(asset, pricesMap);
            }
        }
        return totalUsd;
    }

    private decimal ParseFuturesValue(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0m;
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("totalWalletBalance", out var balanceProp))
        {
            return decimal.TryParse(balanceProp.GetString(), out var futuresUsd) ? futuresUsd : 0m;
        }
        return 0m;
    }

    private decimal ParseEarnValue(string json, Dictionary<string, decimal> pricesMap)
    {
        if (string.IsNullOrEmpty(json)) return 0m;
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("rows", out var rowsElement)) return 0m;

        decimal totalUsd = 0m;
        foreach (var row in rowsElement.EnumerateArray())
        {
            if (row.TryGetProperty("asset", out var assetProp) && row.TryGetProperty("totalAmount", out var amtProp))
            {
                var asset = assetProp.GetString() ?? string.Empty;
                var amtStr = amtProp.GetString() ?? "0";
                if (decimal.TryParse(amtStr, out var volume) && volume > 0)
                {
                    totalUsd += volume * GetAssetPriceInUsdt(asset, pricesMap);
                }
            }
        }
        return totalUsd;
    }

    private decimal GetAssetPriceInUsdt(string asset, Dictionary<string, decimal> pricesMap)
    {
        if (asset is "USDT" or "BUSD" or "USDC") return 1m;
        return pricesMap.TryGetValue($"{asset}USDT", out var price) ? price : 0m;
    }

    #endregion

    private static string ComputeHmacSha256(string message, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(messageBytes)).ToLowerInvariant();
    }

    #region INTERNAL RESPONSE SCHEMAS

    private class BinanceAccountResponse { public List<BinanceBalance>? Balances { get; set; } }
    private class BinanceBalance { public string? Asset { get; set; } public string? Free { get; set; } }
    private class BinanceTickerPrice { public string? Symbol { get; set; } public string? Price { get; set; } }

    #endregion
}