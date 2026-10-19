using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.UpdateRealEstateInstallment;

public record RealEstateInstallmentResultDto(
    Guid AssetId,
    string Name,
    int PaidInstallments,
    int TotalInstallments,
    double CompletionPercentage,
    decimal CurrentEquityPKR,
    decimal OutstandingLiabilityPKR,
    DateTime LastUpdatedUtc
);

public record UpdateRealEstateInstallmentCommand(
    Guid AssetId,
    string UserEmail,
    int PaidInstallments,
    int TotalInstallments,
    decimal? UpdatedValuation = null
) : ICommand<RealEstateInstallmentResultDto>;

public class UpdateRealEstateInstallmentCommandHandler : ICommandHandler<UpdateRealEstateInstallmentCommand, RealEstateInstallmentResultDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateRealEstateInstallmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RealEstateInstallmentResultDto> HandleAsync(UpdateRealEstateInstallmentCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == command.AssetId && a.UserEmail == normalizedEmail, cancellationToken);

        if (asset == null)
            throw new KeyNotFoundException($"Real estate asset with identifier '{command.AssetId}' was not found for this account.");

        // Execute domain method with strict validation
        asset.UpdateInstallments(command.PaidInstallments, command.TotalInstallments, command.UpdatedValuation);

        await _context.SaveChangesAsync(cancellationToken);

        var completionPercentage = command.TotalInstallments > 0
            ? Math.Round((double)command.PaidInstallments / command.TotalInstallments * 100, 2)
            : 0.0;

        // Cashflow calculation: estimate total plot cost from current paid equity ratio
        decimal estimatedTotalValue;
        if (command.PaidInstallments > 0 && command.TotalInstallments > 0)
        {
            var fractionPaid = (decimal)command.PaidInstallments / command.TotalInstallments;
            estimatedTotalValue = Math.Round(asset.HoldingValuePKR / fractionPaid, 2);
        }
        else
        {
            estimatedTotalValue = asset.HoldingValuePKR;
        }

        var outstandingLiability = Math.Max(0m, estimatedTotalValue - asset.HoldingValuePKR);

        return new RealEstateInstallmentResultDto(
            AssetId: asset.Id,
            Name: asset.Name,
            PaidInstallments: asset.InstallmentsPaid ?? command.PaidInstallments,
            TotalInstallments: asset.TotalInstallments ?? command.TotalInstallments,
            CompletionPercentage: completionPercentage,
            CurrentEquityPKR: asset.HoldingValuePKR,
            OutstandingLiabilityPKR: outstandingLiability,
            LastUpdatedUtc: asset.LastUpdatedUtc
        );
    }
}
