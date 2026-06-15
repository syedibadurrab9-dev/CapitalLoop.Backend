using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetAssetNodesList;


/// <summary>
/// 📦 CQRS DATA TRANSFER OBJECT (DTO)
/// Immutable wire data payload capturing descriptive metadata fields and calculated weight vectors.
/// </summary>
public record AssetNodeTelemetryDto(
    Guid NodeId,
    string AssetName,
    string AssetCategory,
    decimal ValuationPkr,
    decimal AllocationPercentage,
    bool IsAutomatedSyncActive,
    DateTime LastSynchronizedAtUtc
);

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Allocation-insulated handler pulling explicit asset metrics and applying inline allocation weighting [CL-201 / CL-203].
/// Fully synchronized with the updated entity layer tracking changes and pluralized database context layout.
/// </summary>
public class GetAssetNodesListQueryHandler : IRequestHandler<GetAssetNodesListQuery, IReadOnlyCollection<AssetNodeTelemetryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAssetNodesListQueryHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyCollection<AssetNodeTelemetryDto>> Handle(GetAssetNodesListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        // 🎯 STEP 1: PERSISTENCE PLURALIZATION REALIGNMENT - Target the true entity collection mapping 'AssetNodes'
        var rawNodes = await _context.AssetNodes
            .AsNoTracking()
            .Where(a => a.UserEmail == sanitizedEmail)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Category,
                a.ValuationPkr, // 🛠️ FIXED: Realigned to use your true domain model field name
                a.IsAutoSynced,
                // Fallback handling to verify structural timestamps safely
                LastModified = EF.Property<DateTime>(a, "LastModified")
            })
            .ToListAsync(cancellationToken);

        if (!rawNodes.Any())
        {
            return Array.Empty<AssetNodeTelemetryDto>();
        }

        // 🎯 STEP 2: DYNAMIC WEIGHT COMPUTATIONS - Sum structural metrics cleanly using localized variable names
        decimal totalNetWorth = rawNodes.Sum(n => n.ValuationPkr);

        // Map and project records dynamically into unified immutable telemetry contracts
        var projectedDtos = rawNodes.Select(n => new AssetNodeTelemetryDto(
            NodeId: n.Id,
            AssetName: n.Name,
            AssetCategory: n.Category.ToString(),
            ValuationPkr: Math.Round(n.ValuationPkr, 2),
            AllocationPercentage: totalNetWorth > 0 ? Math.Round((n.ValuationPkr / totalNetWorth) * 100, 2) : 0,
            IsAutomatedSyncActive: n.IsAutoSynced,
            LastSynchronizedAtUtc: n.LastModified
        )).ToList();

        return projectedDtos.AsReadOnly();
    }
}