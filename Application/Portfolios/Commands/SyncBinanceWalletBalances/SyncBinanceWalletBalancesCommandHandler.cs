using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Interfaces.ExternalIntegrations;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Commands.SyncBinanceWalletBalances;

/// <summary>
/// ⚙️ CQRS BATCH COMMAND HANDLER ORCHESTRATION LAYER
/// Runs scheduled background sweeps over stale automated crypto exchange wallets,
/// isolating iteration faults while committing synchronized changes in optimized transactional batches [CL-405 / CL-414].
/// </summary>
public class SyncBinanceWalletBalancesCommandHandler : IRequestHandler<SyncBinanceWalletBalancesCommand, BinanceBatchSyncResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICryptoSyncService _cryptoSyncService;
    private readonly ILogger<SyncBinanceWalletBalancesCommandHandler> _logger;

    public SyncBinanceWalletBalancesCommandHandler(
        IApplicationDbContext context,
        ICryptoSyncService cryptoSyncService,
        ILogger<SyncBinanceWalletBalancesCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cryptoSyncService = cryptoSyncService ?? throw new ArgumentNullException(nameof(cryptoSyncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BinanceBatchSyncResultDto> Handle(SyncBinanceWalletBalancesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation("Background Automated Wallet Sync Initiated | Max Batch Constraints: {BatchSize}", command.BatchSize);

        // 🎯 STEP 1: PERSISTENCE ALIGNMENT - Query using the realigned context table property 'AssetNodes'
        var staleWallets = await _context.AssetNodes
            .Where(a => a.Category == AssetCategory.Crypto && a.IsAutoSynced && a.EncryptedApiKey != null && a.EncryptedApiKey != "")
            .OrderBy(a => a.Id)
            .Take(command.BatchSize)
            .ToListAsync(cancellationToken);

        if (!staleWallets.Any())
        {
            _logger.LogInformation("Automated Wallet Sync Complete | Zero stale third-party wallet integrations identified for update.");
            return new BinanceBatchSyncResultDto(ProcessedCount: 0, SuccessfulCount: 0, FailedCount: 0);
        }

        int successCount = 0;
        int failureCount = 0;

        // Loop through the batch iteratively to maintain fault isolation barriers
        foreach (var walletNode in staleWallets)
        {
            try
            {
                // 🛠️ FIXED CS8604 NULLABILITY GATES: Localize the key pointer reference tracking type and explicitly verify
                var secureApiKey = walletNode.EncryptedApiKey;

                if (string.IsNullOrWhiteSpace(secureApiKey))
                {
                    _logger.LogWarning("Exchange Batch Sync Interrupted | Node identifier token '{AssetId}' is structurally unassigned or null. Skipping record.", walletNode.Id);
                    failureCount++;
                    continue;
                }

                // 🎯 STEP 2: METRIC SYNCHRONIZATION VIA EXACT MATRIX METHOD - FetchLiveWalletBalanceAsync
                // The localized variable 'secureApiKey' is guaranteed non-null, completely satisfying static code analysis
                decimal updatedLiveBalancePkr = await _cryptoSyncService.FetchLiveWalletBalanceAsync(secureApiKey, cancellationToken);

                if (updatedLiveBalancePkr >= 0)
                {
                    // =============================================================================
                    // 🧱 RESOLVES CS7036: PASS SYSTEM OPERATOR FOOTPRINT TO MUTATOR
                    // =============================================================================
                    // Added 'walletNode.UserEmail' as the third tracking argument to completely satisfy your domain's audit constraints.
                    walletNode.HydrateLiveBalanceFromApi(updatedLiveBalancePkr, secureApiKey, walletNode.UserEmail);
                    successCount++;
                }
                else
                {
                    _logger.LogWarning("Exchange Batch Sync Interrupted | Uncalculated or negative balance returned for asset node: {AssetId}", walletNode.Id);
                    failureCount++;
                }
            }
            catch (Exception ex)
            {
                // Catching errors inside the iteration guarantees a revoked or expired API key won't crash the whole worker engine
                _logger.LogError(ex, "Critical failure encountered syncing automated wallet node {AssetId} for user {UserEmail}", walletNode.Id, walletNode.UserEmail);
                failureCount++;
            }
        }

        // 🎯 STEP 3: TRANSACTIONAL BATCH PERSISTENCE - Commit modifications in a single transaction block save
        if (successCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Background Automated Wallet Sync Complete | Processed: {Total} | Succeeded: {Success} | Failed: {Failed}",
            staleWallets.Count, successCount, failureCount);

        return new BinanceBatchSyncResultDto(
            ProcessedCount: staleWallets.Count,
            SuccessfulCount: successCount,
            FailedCount: failureCount
        );
    }
}