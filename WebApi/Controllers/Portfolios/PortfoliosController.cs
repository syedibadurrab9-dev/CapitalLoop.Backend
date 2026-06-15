using Application.Common.Interfaces;
using Application.Portfolios.Commands;
using Application.Portfolios.Commands.OverrideUserPlan;
using Application.Portfolios.Commands.SharePortfolioLink;
using Application.Portfolios.Queries;
using Application.Portfolios.Queries.GetAssetNodesList;
using Application.Portfolios.Queries.GetSubscriptionPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;
using WebApi.Filters;

namespace WebApi.Controllers.Portfolios;

/// <summary>
/// 💼 ASSET MANAGEMENT & LEDGER CORE CONTROLLER
/// Coordinates the creation of manual ledger asset blocks, portfolio share links, 
/// financial health aggregations, and back-office subscription configurations [CL-201 / CL-203].
/// </summary>
[Authorize]
public class PortfoliosController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public PortfoliosController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Creates a localized manual tracking ledger node for assets like traditional silver/gold or property rows [CL-203].
    /// </summary>
    [HttpPost("assets/create-manual")]
    [SubscriptionRequirement("Manual_Ledger_Tracking")] // 🔒 FEATURE GATE: Permits standard-free, premium, advisor-wealth, tax-consultant, and agent-backoffice globally
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateManualAssetNode([FromBody] CreateManualAssetNodeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // 🎯 SECURITY ALIGNMENT: Resolves user ownership context parameters straight from system claims
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        var authenticatedCommand = command with { UserEmail = currentUserEmail };

        var generatedNodeId = await Mediator.Send(authenticatedCommand);
        return CreatedAtAction(nameof(GetAssetNodesList), new { id = generatedNodeId }, generatedNodeId);
    }

    /// <summary>
    /// Generates a cryptographically signed, read-only snapshot pathway sharing access key capped at 30 days [CL-209].
    /// </summary>
    [HttpPost("share-link/generate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    public async Task<IActionResult> SharePortfolioLink([FromBody] SharePortfolioLinkCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        var authenticatedCommand = command with { UserEmail = currentUserEmail };

        var generatedUrlPath = await Mediator.Send(authenticatedCommand);
        return Ok(generatedUrlPath);
    }

    /// <summary>
    /// Back-office admin tool that updates subscriber plans and alters identity security claims [CL-503].
    /// </summary>
    [Authorize(Roles = "Admin, SystemSupervision")] // Locked down strictly to administrative credentials
    [HttpPost("admin/override-plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> OverrideUserPlan([FromBody] OverrideUserPlanCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    // =============================================================================
    // 📈 QUERY ROUTING CHANNELS (IDEMPOTENT TELEMETRY FEEDS)
    // =============================================================================

    /// <summary>
    /// Aggregates overall multi-wallet asset positioning data into high-speed financial dashboards.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolioSummary()
    {
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetPortfolioSummaryQuery(UserEmail: currentUserEmail)));
    }

    /// <summary>
    /// Pulls all active manual rows or integrated multi-asset inventory nodes tied to the executing profile.
    /// </summary>
    [HttpGet("assets/list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssetNodesList()
    {
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetAssetNodesListQuery(UserEmail: currentUserEmail)));
    }

    /// <summary>
    /// Returns available system tier layout structures and operational features.
    /// </summary>
    [HttpGet("subscription-plans")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        return Ok(await Mediator.Send(new GetSubscriptionPlansQuery()));
    }
}