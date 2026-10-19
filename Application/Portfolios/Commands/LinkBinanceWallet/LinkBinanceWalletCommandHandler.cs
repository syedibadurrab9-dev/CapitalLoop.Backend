using Application.Common.DTOs;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.LinkBinanceWallet;

public class LinkBinanceWalletCommandHandler : ICommandHandler<LinkBinanceWalletCommand, BinanceSyncResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICryptoSyncService _cryptoSyncService;
    private readonly IValidator<LinkBinanceWalletCommand>? _validator;

    public LinkBinanceWalletCommandHandler(
        IApplicationDbContext context,
        ICryptoSyncService cryptoSyncService,
        IValidator<LinkBinanceWalletCommand>? validator = null)
    {
        _context = context;
        _cryptoSyncService = cryptoSyncService;
        _validator = validator;
    }

    public async Task<BinanceSyncResult> HandleAsync(LinkBinanceWalletCommand command, CancellationToken cancellationToken)
    {
        if (_validator != null)
        {
            await _validator.ValidateAndThrowAsync(command, cancellationToken);
        }

        // 1. Handshake with the infrastructure-level integration service to query Binance endpoints securely
        var syncResult = await _cryptoSyncService.FetchExtendedPortfolioBalancesAsync(command.ApiKey, command.SecretKey);

        // 2. Query the DB context to look for an existing auto-synced Crypto node for this user profile
        var existingCryptoNode = await _context.Assets
            .FirstOrDefaultAsync(n => n.UserEmail == command.UserEmail.ToLowerInvariant()
                                 && n.Category == AssetCategory.Crypto
                                 && n.IsAutoSynced, cancellationToken);

        if (existingCryptoNode != null)
        {
            // Update the existing model's aggregated net worth tracking values
            existingCryptoNode.HydrateLiveBalanceFromApi(syncResult.TotalValuePkr);
        }
        else
        {
            // Construct a pristine, tracking-ready asset tree node for new integrations
            var binanceNode = AssetNode.CreateManual(
                email: command.UserEmail,
                name: "Binance Integrated Wallet",
                category: AssetCategory.Crypto,
                value: syncResult.TotalValuePkr
            );

            // Re-hydrate state metadata parameters explicitly to flag it as an automated synchronization channel
            binanceNode.HydrateLiveBalanceFromApi(syncResult.TotalValuePkr);

            _context.Assets.Add(binanceNode);
        }

        // 3. Persist structural tracking records safely to your database instance
        await _context.SaveChangesAsync(cancellationToken);

        // Return calculated asset splits directly back to the controller to feed React Native charts
        return syncResult;
    }
}