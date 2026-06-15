using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Portfolios.Commands;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Execution Handler mapping command parameters safely into business asset aggregate variants [CL-201 / CL-203].
/// Fully synchronized with your master persistence table schema 'AssetNodes'.
/// </summary>
public class CreateManualAssetNodeCommandHandler : IRequestHandler<CreateManualAssetNodeCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateManualAssetNodeCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Guid> Handle(CreateManualAssetNodeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        AssetNode assetNode;
        var operationalPrincipal = command.UserEmail.Trim().ToLowerInvariant();

        // 🎛️ FACTORY ROUTING MATRIX: Direct parameters into the specialized domain constructor pathing
        if (command.Category == AssetCategory.PreciousMetal)
        {
            if (!command.TotalTolas.HasValue || !command.CostPerTola.HasValue)
            {
                throw new ArgumentException("Asset entry rejected: Total Tolas and Cost per Tola parameters must be provided for precious metal commodities.");
            }

            // Invokes the precise tola weight factory method. SetCreationAuditTelemetry fires inside natively.
            assetNode = AssetNode.CreateCommodityTola(
                email: operationalPrincipal,
                name: command.Name,
                tolas: command.TotalTolas.Value,
                costPerTola: command.CostPerTola.Value);
        }
        else
        {
            // Invokes the general manual position ledger initialization pipeline factory method.
            assetNode = AssetNode.CreateManual(
                email: operationalPrincipal,
                name: command.Name,
                category: command.Category,
                value: command.HoldingValuePkr,
                paid: command.InstallmentsPaid,
                total: command.TotalInstallments);
        }

        // 🛠️ FIXED: Staged over your updated, synchronized DbSet reference point 'AssetNodes'
        await _context.AssetNodes.AddAsync(assetNode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return assetNode.Id;
    }
}