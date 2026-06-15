using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.MarketIntelligence;
using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.AiAdvisor.Queries.GetAssetAllocationDeficiencies;


/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-Performance Execution Handler evaluating multi-asset distribution imbalances across user nodes [CL-203].
/// Fully realigned with updated AssetNode domain models and cross-linked with IMarketDataService matrix operations.
/// </summary>
public class GetAssetAllocationDeficienciesQueryHandler : IRequestHandler<GetAssetAllocationDeficienciesQuery, AllocationAnalysisDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMarketDataService _marketDataService;

    public GetAssetAllocationDeficienciesQueryHandler(
        IApplicationDbContext context,
        IMarketDataService marketDataService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _marketDataService = marketDataService ?? throw new ArgumentNullException(nameof(marketDataService));
    }

    public async Task<AllocationAnalysisDto> Handle(GetAssetAllocationDeficienciesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🚀 HIGH-SPEED LOOKUP: Resolve profile identity indicators needed to feed market intelligence engines
        var userProfile = await _context.UserProfiles
            .AsNoTracking()
            .Where(p => p.Email == sanitizedEmail)
            .Select(p => new { p.Id, p.IsFiler })
            .FirstOrDefaultAsync(cancellationToken);

        if (userProfile is null)
        {
            throw new InvalidOperationException($"Asset analysis aborted: Profile context tracking row could not be found for identity '{sanitizedEmail}'.");
        }

        // Fetch user positions using your realigned, pluralized fast read-only query stream
        var userAssets = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new { a.Category, a.ValuationPkr }) // 🛠️ FIXED: Realigned to map to your true entity property bounds
            .ToListAsync(cancellationToken);

        if (!userAssets.Any())
        {
            return new AllocationAnalysisDto(
                TotalNetWorthPkr: 0,
                DetectedDiscrepancies: new List<string> { "No active portfolio asset nodes are registered under this profile context. Ingest manual positions or link external API wallets to initiate diagnostic checks." }.AsReadOnly(),
                IsRebalancingActionRequired: false
            );
        }

        // 🎯 STEP 1: INTERFACE MATRIX COHESION - Leverage the specialized drift vector analysis engine natively
        var explicitDiscrepancies = new List<string>();
        try
        {
            var telemetryAnalysis = await _marketDataService.AnalyzeDriftVectorsAsync(userProfile.Id);
            if (telemetryAnalysis is not null && telemetryAnalysis.DetectedDiscrepancies.Any())
            {
                explicitDiscrepancies.AddRange(telemetryAnalysis.DetectedDiscrepancies);
            }
        }
        catch (Exception)
        {
            // Fall back gracefully to local execution rules if the external telemetry engine channel experiences downtime
        }

        // 🎯 STEP 2: LOCAL RULES EVALUATION - Execute local baseline financial rules natively
        decimal totalNetWorth = userAssets.Sum(a => a.ValuationPkr);
        decimal cryptoValue = userAssets.Where(a => a.Category == AssetCategory.Crypto).Sum(a => a.ValuationPkr);
        decimal cryptoRatio = totalNetWorth > 0 ? (cryptoValue / totalNetWorth) * 100 : 0;

        // Regulatory Safeguard Guardrail: Warn retail profiles if speculative assets breach 15% of total net holdings
        if (cryptoRatio > 15)
        {
            explicitDiscrepancies.Add($"High Risk Concentration Warning: Crypto positions compromise {cryptoRatio:F2}% of your total asset pool, outpacing the standard 15% secure portfolio cap threshold.");
        }

        // Local Tax Optimization Guardrail: Evaluate asset choices against active tax filing roles
        if (!userProfile.IsFiler)
        {
            decimal equityValue = userAssets
                .Where(a => a.Category == AssetCategory.PsxEquity || a.Category == AssetCategory.MutualFund)
                .Sum(a => a.ValuationPkr);

            if (equityValue == 0)
            {
                explicitDiscrepancies.Add("Tax Optimization Deficiency: Your profile is categorized as a non-filer with zero corporate stock or mutual fund index placements. Placements in mutual funds can offer enhanced withholding tax advantages under current local laws.");
            }
        }

        // Remove any duplicated warnings that might have trickled out from both layers simultaneously
        var distinctDiscrepancies = explicitDiscrepancies.Distinct().ToList();

        return new AllocationAnalysisDto(
            TotalNetWorthPkr: Math.Round(totalNetWorth, 2),
            DetectedDiscrepancies: distinctDiscrepancies.AsReadOnly(),
            IsRebalancingActionRequired: distinctDiscrepancies.Any()
        );
    }
}