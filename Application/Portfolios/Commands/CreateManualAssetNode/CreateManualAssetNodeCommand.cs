using System;
using Domain.Enums;
using MediatR;

namespace Application.Portfolios.Commands;

/// <summary>
/// CQRS Command to provision a manual wealth tracking position [CL-203].
/// Supports standard investments, real estate payment schedules, or structural commodity tolas.
/// </summary>
/// <param name="UserEmail">The authenticated profile email tracking the ownership layout.</param>
/// <param name="Name">The asset description moniker or ticker identifier designation.</param>
/// <param name="Category">The broad financial asset categorization enum type.</param>
/// <param name="HoldingValuePkr">The fiat valuation or historical acquisition balance calculated in PKR.</param>
/// <param name="InstallmentsPaid">Optional completed payment schedule milestones for installment property files.</param>
/// <param name="TotalInstallments">Optional master baseline capacity limits for installment property files.</param>
/// <param name="TotalTolas">Optional material volume specification metrics required for precious metals tracking.</param>
/// <param name="CostPerTola">Optional historical conversion rate pricing parameters required for precious metals tracking.</param>
public record CreateManualAssetNodeCommand(
    string UserEmail,
    string Name,
    AssetCategory Category,
    decimal HoldingValuePkr,
    int? InstallmentsPaid = null,
    int? TotalInstallments = null,
    decimal? TotalTolas = null,
    decimal? CostPerTola = null) : IRequest<Guid>;