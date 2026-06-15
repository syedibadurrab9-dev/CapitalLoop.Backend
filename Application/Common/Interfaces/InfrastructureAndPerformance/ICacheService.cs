using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

/// <summary>
/// 🔄 DISTRIBUTED MEMORY ACCELERATION INTERFACE CONTRACT
/// Abstracts infrastructure caching layers (e.g., Redis Cluster Pool) away from business use cases [CL-101 / CL-503].
/// Used to accelerate query performance and manage transactional identity session invalidations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves a strongly-typed value matching the designated key string token.
    /// Natively utilized by GetLiveChartsDataQuery and GetSuitabilityQuestionsQuery.
    /// </summary>
    /// <typeparam name="T">The data mapping contract format to deserialize into.</typeparam>
    /// <param name="key">The unique tracking key mapping back to memory storage.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The cached entity if discovered; otherwise, the structural default value.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a strongly-typed data payload to the distributed cache layer with an explicit sliding expiration window.
    /// </summary>
    /// <typeparam name="T">The data tracking object type to serialize into memory.</typeparam>
    /// <param name="key">The unique key pointer identifier mapping to the cache entry.</param>
    /// <param name="value">The actual target data structure to be cached.</param>
    /// <param name="expiration">An optional custom lifetime duration before the cache block is automatically pruned.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 🚀 ECOSYSTEM SESSION SECURITY INVALIDATOR [CL-101]
    /// Instantly evicts a key string allocation from the active cache storage partition.
    /// Natively called by OverrideUserPlanCommand to flush outdated subscriber dashboards.
    /// </summary>
    /// <param name="key">The specific memory target key to purge.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    Task EvictAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Alternate fallback signature mapping to standard infrastructure provider naming mechanics.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default) => EvictAsync(key, cancellationToken);

    /// <summary>
    /// Checks for the structural existence of a key reference across memory layers without fetching its payload.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}