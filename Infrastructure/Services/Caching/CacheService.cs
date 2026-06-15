using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Caching;

/// <summary>
/// ⚡ HIGH-PERFORMANCE REDIS CACHING GATEWAY
/// Concrete service handling text stream serialization pipelines to lower database read tension [CL-101 / CL-503].
/// Fully conforms to all structural contract mappings defined in ICacheService.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxAbsoluteExpiration = TimeSpan.FromDays(1);

    public CacheService(IDistributedCache distributedCache, ILogger<CacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a strongly-typed value matching the designated key string token.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return default;

        try
        {
            var cachedString = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cachedString))
            {
                _logger.LogDebug("[CACHE-SERVICE] Cache miss encountered for key pointer: '{Key}'", key);
                return default;
            }

            _logger.LogDebug("[CACHE-SERVICE] Cache hit successfully resolved for key pointer: '{Key}'", key);

            return JsonSerializer.Deserialize<T>(cachedString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CACHE-SERVICE] Exception processing deserialization pipeline for key: '{Key}'", key);
            return default;
        }
    }

    /// <summary>
    /// Persists a strongly-typed data payload to the distributed cache layer with an explicit sliding and absolute expiration window.
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        if (value is null) return;

        try
        {
            var slidingWindow = expiration ?? DefaultSlidingExpiration;

            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingWindow,
                AbsoluteExpirationRelativeToNow = slidingWindow > MaxAbsoluteExpiration ? slidingWindow : MaxAbsoluteExpiration
            };

            var serializedString = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, serializedString, options, cancellationToken);

            _logger.LogDebug("[CACHE-SERVICE] Key allocation committed cleanly into memory partition: '{Key}'", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CACHE-SERVICE] Serialization breakdown failed to write key node: '{Key}'", key);
        }
    }

    /// <summary>
    /// 🚀 ECOSYSTEM SESSION SECURITY INVALIDATOR [CL-101]
    /// Instantly evicts a key string allocation from the active cache storage partition.
    /// </summary>
    public async Task EvictAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        _logger.LogInformation("[CACHE-SERVICE] Security Invalidation Target Evicted: '{Key}'", key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Checks for the structural existence of a key reference across memory layers without fetching its payload.
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        try
        {
            // 🧱 FIX CS1061: Swapped synchronous .Get() to asynchronous .GetAsync() to complement the await expression cleanly
            var valueBytes = await _distributedCache.GetAsync(key, cancellationToken);
            return valueBytes is not null && valueBytes.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CACHE-SERVICE] Structural checking anomaly encountered on key: '{Key}'", key);
            return false;
        }
    }
}