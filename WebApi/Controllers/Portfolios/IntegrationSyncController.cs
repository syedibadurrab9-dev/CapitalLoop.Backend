using System;
using System.Threading.Tasks;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands.LinkBinanceWallet;
using Application.Portfolios.Commands.SyncBinanceWalletBalances;
using Application.Portfolios.Commands.TriggerApiSync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using WebApi.Filters;

namespace WebApi.Controllers.Portfolios;

/// <summary>
/// 🔄 EXTERNAL INTEGRATION & MARKET SYNC CONTROLLER
/// Coordinates third-party exchange credentials linking, automated asset balance pushes, 
/// and real-time ledger node valuation sync triggers [CL-201].
/// </summary>
[Authorize]
public class IntegrationSyncController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public IntegrationSyncController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Establishes and verifies a third-party exchange profile integration pipeline [CL-201].
    /// </summary>
    [HttpPost("binance/link")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Guarded by premium, advisor-wealth, and agent-backoffice policies
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BinanceSyncResultDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LinkBinanceWallet([FromBody] LinkBinanceWalletCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 🎯 SECURITY ALIGNMENT: Forces active token tracking identity parameters onto the command execution matrix
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // =============================================================================
        // 🧱 RESOLVES CS8852: USE 'WITH' MUTATION FOR IMMUTABLE INIT-ONLY RECORDS
        // =============================================================================
        // Generates a clean shallow projection of the record, overriding the security footprint boundary safely.
        var authenticatedCommand = command with { UserEmail = currentUserEmail };

        var result = await Mediator.Send(authenticatedCommand);
        return result.IsSyncSuccessful ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Triggers manual, immediate data portal scrapers or REST calls to update asset nodes.
    /// </summary>
    [HttpPost("manual-sync")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Ensures live DB check permits external scrapers
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> TriggerApiSync([FromBody] TriggerApiSyncCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Ensure multi-tenant isolation rules by overlaying the active tracking user claim
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🧱 RESOLVES CS8852: Securely assign token context via with expressions
        var authenticatedCommand = command with { UserEmail = currentUserEmail };

        return Ok(await Mediator.Send(authenticatedCommand));
    }

    /// <summary>
    /// Background task routing channel to run multi-asset portfolio batch sweeps.
    /// Natively called by automated system workers or administrative cron loops.
    /// </summary>
    [HttpPost("binance/batch-sync")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Protects background engine pools from unmonetized triggers
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BinanceBatchSyncResultDto))]
    public async Task<IActionResult> SyncBinanceWalletBalances([FromBody] SyncBinanceWalletBalancesCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Dispatches the batch sync command straight to its high-throughput handler block
        var batchResult = await Mediator.Send(command);
        return Ok(batchResult);
    }
}