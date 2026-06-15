using Application.Common.Interfaces;
using Application.Common.Interfaces.MarketIntelligence;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Queries;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-Performance Execution Handler processing time-series wealth growth models based on user risk profiles [CL-302].
/// Integrates live price feeds via your updated IMarketDataService matrix using true AssetNode domain properties.
/// </summary>
public class GetForecastProjectionsQueryHandler : IRequestHandler<GetForecastProjectionsQuery, ForecastProjectionsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<GetForecastProjectionsQueryHandler> _logger;

    public GetForecastProjectionsQueryHandler(
        IApplicationDbContext context,
        IMarketDataService marketDataService,
        ILogger<GetForecastProjectionsQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ForecastProjectionsDto> Handle(GetForecastProjectionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation(
            "Generating Live Wealth Forecast | Target: {Email} over {Months} Months [CL-302]",
            sanitizedEmail, query.ProjectionMonths);

        // 🚀 EFFICIENCY STEP 1: Fetch user risk vectors using a non-tracked lookup profile matrix
        var profile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.Email == sanitizedEmail)
            .Select(p => new { p.SubscriptionTier, p.SuitabilityAssessmentScore })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"Forecast simulation aborted: Target profile context could not be resolved for identity '{sanitizedEmail}'.");
        }

        // 🛡️ PREMIUM FEATURE GATE: Limit advanced 120-month forecasting models to premium membership tiers
        bool isPremiumAuthorized = SubscriptionTiers.IsFeatureAuthorized(profile.SubscriptionTier, SubscriptionTiers.StrategicRoboInsights);

        int enforcedTimeline = query.ProjectionMonths;
        if (query.ProjectionMonths > 36 && !isPremiumAuthorized)
        {
            _logger.LogWarning("Subscription Gate Triggered | Free tier restricted to 36-month horizon limits for user: {Email}", sanitizedEmail);
            enforcedTimeline = 36; // Gracefully restrict the timeline matrix rather than breaking the UI thread
        }

        // 🚀 EFFICIENCY STEP 2: Pull active holding structures to execute live revaluation handshakes
        var activeHoldings = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new { a.Symbol, a.Category, a.QuantityHoldings, a.ValuationPkr })
            .ToListAsync(cancellationToken);

        // 🎯 LIVE MARKET DATA INTEGRATION: Calculate high-fidelity baseline net worth using unified spot prices
        decimal dynamicBaselinePkr = 0;

        foreach (var holding in activeHoldings)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(holding.Symbol))
                {
                    decimal livePrice = await _marketDataService.GetLiveSpotPriceAsync(holding.Symbol, cancellationToken);
                    dynamicBaselinePkr += livePrice * holding.QuantityHoldings;
                }
                else
                {
                    dynamicBaselinePkr += holding.ValuationPkr;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live pricing query bypassed for tracking symbol '{Symbol}'. Falling back to static ledger valuation.", holding.Symbol);
                dynamicBaselinePkr += holding.ValuationPkr;
            }
        }

        // 🧠 MODELING ALGORITHMIC ESTIMATION: Assign an annualized compounding return rate based on risk profile
        double annualizedGrowthRate = 0.10; // Default 10% baseline return rate
        if (profile.SuitabilityAssessmentScore > 70)
        {
            annualizedGrowthRate = 0.18; // 18% aggressive allocation growth target profile
        }
        else if (profile.SuitabilityAssessmentScore >= 30)
        {
            annualizedGrowthRate = 0.14; // 14% balanced accumulation rate
        }

        double monthlyGrowthMultiplier = Math.Pow(1.0 + annualizedGrowthRate, 1.0 / 12.0);

        var timeIntervalLabels = new List<string>();
        var projectedBalancesPkr = new List<decimal>();

        decimal runningValueTracker = dynamicBaselinePkr;
        var startingTimePoint = DateTime.UtcNow;

        // Iterate month-by-month or step-by-step to project wealth trends accurately over time
        for (int monthIndex = 0; monthIndex <= enforcedTimeline; monthIndex += 12)
        {
            var targetLabel = startingTimePoint.AddMonths(monthIndex).ToString("yyyy-MM");
            timeIntervalLabels.Add(targetLabel);
            projectedBalancesPkr.Add(Math.Round(runningValueTracker, 2));

            // Compound the asset value month-by-month for the upcoming 12-month annual step segment block
            double compoundSegmentValue = (double)runningValueTracker * Math.Pow(monthlyGrowthMultiplier, 12);
            runningValueTracker = (decimal)compoundSegmentValue;
        }

        string insightSummaryText = isPremiumAuthorized
            ? $"Strategic Forecast Success: Based on an active compounding target configuration of {annualizedGrowthRate * 100}%, your multi-asset portfolio positions are projected to scale cleanly over the requested timeline."
            : "Strategic Note: You are running a Standard Free Tier simulation limited to a 36-month horizon. Upgrade to Premium access to generate full 120-month long-term wealth trajectories.";

        return new ForecastProjectionsDto(
            BaselineNetWorthPkr: dynamicBaselinePkr,
            SimulatedAnnualGrowthRate: (decimal)annualizedGrowthRate,
            TimelineLabels: timeIntervalLabels.AsReadOnly(),
            ProjectedValuesPkr: projectedBalancesPkr.AsReadOnly(),
            AdvisoryNarrative: insightSummaryText,
            IsModelClippedBySubscriptionGate: query.ProjectionMonths > 36 && !isPremiumAuthorized
        );
    }
}