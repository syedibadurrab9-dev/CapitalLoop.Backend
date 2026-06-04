using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using MediatR;
namespace Application.Portfolios.Commands.CreateManualAsset;

public record CreateManualAssetCommand(
string UserEmail,
string Name,
AssetCategory Category,
decimal HoldingValuePKR,
int? InstallmentsPaid,
int? TotalInstallments) : IRequest<Guid>;

public class CreateManualAssetCommandHandler : IRequestHandler<CreateManualAssetCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateManualAssetCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateManualAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = AssetNode.CreateManual(
            request.UserEmail,
            request.Name,
            request.Category,
            request.HoldingValuePKR,
            request.InstallmentsPaid,
            request.TotalInstallments
        );

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
