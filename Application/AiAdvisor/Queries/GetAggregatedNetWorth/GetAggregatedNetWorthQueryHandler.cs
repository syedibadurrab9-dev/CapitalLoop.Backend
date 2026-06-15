using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.MarketIntelligence;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.AiAdvisor.Queries.GetAggregatedNetWorth;

/// <summary>
/// 📝 CQRS INPUT CONTRACT (TRequest)
/// Captures client request parameters to compute an aggregated real-time net worth valuation matrix.
/// </summary>


/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-Performance Execution Handler that aggregates multi-wallet records from the realigned context
/// and outputs a unified financial footprint matrix [CL-302].
/// Integrates live price feeds via IMarketDataService using true AssetNode domain properties.
/// </summary>
public class GetAggregatedNetWorthQueryHandler : IRequestHandler<GetAggregatedNetWorthQuery, AggregatedNetWorthDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<GetAggregatedNetWorthQueryHandler> _logger;

    public GetAggregatedNetWorthQueryHandler(
        IApplicationDbContext context,
        IMarketDataService marketDataService,
        ILogger<GetAggregatedNetWorthQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AggregatedNetWorthDto> Handle(GetAggregatedNetWorthQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation("Processing Net Worth Real-Time Aggregation Loop | Target Profile: {Email} [CL-302]", sanitizedEmail);

        // 🚀 HIGH-SPEED TELEMETRY LOOKUP: Project structural parameters across your synchronized table property 'AssetNodes'
        // Explicitly maps to 'Symbol' and 'QuantityHoldings' matching your true AssetNode.cs fields
        var rawHoldings = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new { a.Symbol, a.Category, a.QuantityHoldings, a.ValuationPkr })
            .ToListAsync(cancellationToken);

        if (!rawHoldings.Any())
        {
            _logger.LogInformation("No active asset registries identified for profile context: {Email}", sanitizedEmail);
            return new AggregatedNetWorthDto(
                TotalNetWorthPkr: 0.00m,
                AssetClassBalances: new Dictionary<string, decimal>(),
                AssetClassPercentages: new Dictionary<string, decimal>(),
                CalculatedAtUtc: DateTime.UtcNow
            );
        }

        // 🎯 STEP 1: LIVE PRICING COHESION LOOP - Process dynamic evaluations via hot spot rates
        var processedAllocations = new List<(string Category, decimal ValuationPkr)>();

        foreach (var holding in rawHoldings)
        {
            string categoryKey = holding.Category.ToString();

            try
            {
                if (!string.IsNullOrWhiteSpace(holding.Symbol))
                {
                    // 🛠️ INTERFACE REALIGNMENT: Invoking 'GetLiveSpotPriceAsync' matching your updated service matrix contract
                    decimal liveSpotPrice = await _marketDataService.GetLiveSpotPriceAsync(holding.Symbol, cancellationToken);
                    decimal activeValuation = liveSpotPrice * holding.QuantityHoldings;

                    processedAllocations.Add((categoryKey, activeValuation));
                }
                else
                {
                    // Fall back cleanly to internally stored ledger valuations if no tracking symbol is configured
                    processedAllocations.Add((categoryKey, holding.ValuationPkr)); // 🛠️ FIXED: Realigned property call bounds
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live pricing stream query bypassed for symbol '{Symbol}'. Falling back to local static ledger values.", holding.Symbol);
                processedAllocations.Add((categoryKey, holding.ValuationPkr));
            }
        }

        // 🧠 STEP 2: FINANCIAL AGGREGATION LOGIC - Sum totals and compute distributed weights
        decimal totalNetWorthPkr = processedAllocations.Sum(a => a.ValuationPkr);

        var classBalances = new Dictionary<string, decimal>();
        var classPercentages = new Dictionary<string, decimal>();

        // Group live allocations dynamically by category string keys
        var groupedCategories = processedAllocations.GroupBy(a => a.Category);

        foreach (var group in groupedCategories)
        {
            decimal categoryTotalPkr = group.Sum(a => a.ValuationPkr);

            classBalances.Add(group.Key, Math.Round(categoryTotalPkr, 2));

            // Prevent divide-by-zero boundary issues if a portfolio contains zero total allocations
            decimal distributionPercentage = totalNetWorthPkr > 0
                ? (categoryTotalPkr / totalNetWorthPkr) * 100
                : 0;

            classPercentages.Add(group.Key, Math.Round(distributionPercentage, 2));
        }

        _logger.LogInformation("Consolidation Concluded Successfully | Real-Time Calculated Total: {NetWorth} PKR", totalNetWorthPkr);

        return new AggregatedNetWorthDto(
            TotalNetWorthPkr: Math.Round(totalNetWorthPkr, 2),
            AssetClassBalances: classBalances,
            AssetClassPercentages: classPercentages,
            CalculatedAtUtc: DateTime.UtcNow
        );
    }
}