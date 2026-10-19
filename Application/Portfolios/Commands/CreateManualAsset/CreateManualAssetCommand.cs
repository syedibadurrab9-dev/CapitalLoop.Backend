using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Infrastructure.Common.Interfaces;

namespace Application.Portfolios.Commands.CreateManualAsset;

public record CreateManualAssetCommand(
    string UserEmail,
    string Name,
    AssetCategory Category,
    decimal HoldingValuePKR,
    int? InstallmentsPaid,
    int? TotalInstallments) : ICommand<Guid>;

public class CreateManualAssetCommandHandler : ICommandHandler<CreateManualAssetCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateManualAssetCommand>? _validator;

    public CreateManualAssetCommandHandler(
        IApplicationDbContext context,
        IValidator<CreateManualAssetCommand>? validator = null)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Guid> HandleAsync(CreateManualAssetCommand command, CancellationToken cancellationToken)
    {
        if (_validator != null)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);
        }

        var asset = AssetNode.CreateManual(
            command.UserEmail,
            command.Name,
            command.Category,
            command.HoldingValuePKR,
            command.InstallmentsPaid,
            command.TotalInstallments
        );

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
