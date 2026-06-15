using Application.Common.Interfaces.ExternalIntegrations;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.AiAdvisor.Queries.GetLiveChartsData;

/// <summary>
/// 📊 INLINE DATA DTO
/// </summary>
public record ChartPointDto(
    string AssetTicker,
    string AssetName,
    decimal UnitPricePkr,
    DateTime LastSyncedUtc
);

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Fully implements the exact matrix requirements specified in image_365920.png by orchestrating
/// cache intercept lookups, direct metal integrations, and persistence storage reads.
/// </summary>
public class GetLiveChartsDataQueryHandler : IRequestHandler<GetLiveChartsDataQuery, LiveChartsDataDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IPreciousMetalSyncService _metalSyncService;
    private readonly ILogger<GetLiveChartsDataQueryHandler> _logger;

    public GetLiveChartsDataQueryHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        IPreciousMetalSyncService metalSyncService,
        ILogger<GetLiveChartsDataQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _metalSyncService = metalSyncService ?? throw new ArgumentNullException(nameof(metalSyncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LiveChartsDataDto> Handle(GetLiveChartsDataQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();
        string cacheKey = $"charts:telemetry:{sanitizedEmail}";

        _logger.LogInformation("Processing Live Chart Telemetry Pipeline | Profile Target: {Email} [CL-302]", sanitizedEmail);

        // 🎯 STEP 1: LAYER OVER INTERCEPT ACCORDING TO SYSTEM SPECS - Query Distributed Performance Cache
        try
        {
            var cachedData = await _cacheService.GetAsync<LiveChartsDataDto>(cacheKey, cancellationToken);
            if (cachedData is not null)
            {
                _logger.LogInformation("Performance Cache Hit! Returning pre-rendered chart arrays for user: {Email}", sanitizedEmail);
                return cachedData;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transient Redis cache read failure. Falling back to live integration streams dynamically.");
        }

        // 🎯 STEP 2: METRIC INJECTION VIA EXTERNAL INTEGRATIONS - Fetch spot price data using IPreciousMetalSyncService
        decimal physicalGoldSpotRate = 0;
        try
        {
            physicalGoldSpotRate = await _metalSyncService.FetchSpotRatePerTolaAsync("GOLD");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve real-time spot pricing arrays from Precious Metal sync channels.");
        }

        // 🎯 STEP 3: PERSISTENCE FALLBACK READS - Query tracking data pools via IApplicationDbContext
        var pricesFeed = await _context.MarketAssetPrices
            .AsNoTracking()
            .OrderByDescending(p => p.LastSyncedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (!pricesFeed.Any() && physicalGoldSpotRate == 0)
        {
            _logger.LogWarning("Telemetry feeds entirely absent. Returning empty chart template structure.");
            return new LiveChartsDataDto(ChartPoints: Array.Empty<ChartPointDto>(), GeneratedAtUtc: DateTime.UtcNow);
        }

        // Project persistent data feeds to UI wire models
        var chartPoints = pricesFeed
            .OrderBy(p => p.LastSyncedUtc)
            .Select(point => new ChartPointDto(
                AssetTicker: point.AssetTicker,
                AssetName: point.AssetName,
                UnitPricePkr: Math.Round(point.UnitPricePkr, 2),
                LastSyncedUtc: point.LastSyncedUtc
            ))
            .ToList();

        // Dynamically append the freshly pulled metal integration ticker mark if valid
        if (physicalGoldSpotRate > 0)
        {
            chartPoints.Add(new ChartPointDto(
                AssetTicker: "GOLD",
                AssetName: "Physical Pure Gold Spot Rate",
                UnitPricePkr: Math.Round(physicalGoldSpotRate, 2),
                LastSyncedUtc: DateTime.UtcNow
            ));
        }

        var finalizedResult = new LiveChartsDataDto(
            ChartPoints: chartPoints.AsReadOnly(),
            GeneratedAtUtc: DateTime.UtcNow
        );

        // 🎯 STEP 4: MEMORY ACCELERATION HYDRATION - Commit calculated payload back into cache with a 5-minute window
        try
        {
            await _cacheService.SetAsync(cacheKey, finalizedResult, TimeSpan.FromMinutes(5), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not commit fresh chart calculations to distributed memory partition.");
        }

        return finalizedResult;
    }
}