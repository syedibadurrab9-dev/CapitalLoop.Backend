using Application.AiAdvisor.Queries.GetAggregatedNetWorth;
using Application.AiAdvisor.Queries.GetAssetAllocationDeficiencies;
using Application.AiAdvisor.Queries.GetLiveChartsData;
using Application.AiAdvisor.Queries.GetRiskAuditSummary;
using Application.AiAdvisor.Queries.GetRoboAdvisorySnapshot;
using Application.AiAdvisor.Queries.GetSuitabilityQuestions;
using Application.AiAdvisor.Queries.GetTargetRecommendations;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Portfolios.Commands;
using Application.Portfolios.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;
using WebApi.Filters;

namespace WebApi.Controllers.AiAdvisor;

/// <summary>
/// 🧠 INTELLIGENT ROBO-ADVISORY & PERFORMANCE CONTROLLER
/// Handles suitability assessments, automated wealth building queries, drift vectors, and portfolio simulations.
/// Fully covers all specified AiAdvisor feature endpoints, resolving context parameters via identity abstractions.
/// </summary>
[Authorize]
public class AdvisorController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Injects the central identity engine to resolve user credentials from the HTTP context claims matrix.
    /// </summary>
    public AdvisorController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Processes user profiling risk variables and commits suitability parameters to local aggregates.
    /// </summary>
    [HttpPost("suitability/evaluate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluateSuitability([FromBody] EvaluateSuitabilityCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    // =============================================================================
    // 📈 QUERY ROUTING CHANNELS (IDEMPOTENT TELEMETRY FEEDS)
    // =============================================================================

    /// <summary>
    /// Computes consolidated aggregate net-worth valuations across active ledger nodes [CL-201/203].
    /// </summary>
    [HttpGet("net-worth/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAggregatedNetWorth()
    {
        // 🛠️ FIXED: Resolved parameter CS7036 by pulling email from the active claims principal
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetAggregatedNetWorthQuery(currentUserEmail)));
    }

    /// <summary>
    /// Returns the central robo-advisory operational matrix workspace snapshot.
    /// </summary>
    [HttpGet("robo-snapshot")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoboAdvisorySnapshot()
    {
        // 🛠️ FIXED: Passed required parameter context
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetRoboAdvisorySnapshotQuery(currentUserEmail)));
    }

    /// <summary>
    /// Intercepts active tracking allocations to diagnose portfolio target asset drift vectors.
    /// </summary>
    [HttpGet("allocation/deficiencies")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Guarded by premium, advisor-wealth, and agent-backoffice policies
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AllocationAnalysisDto))]
    public async Task<IActionResult> GetAssetAllocationDeficiencies()
    {
        // 🛠️ FIXED: Passed required parameter context
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetAssetAllocationDeficienciesQuery(currentUserEmail)));
    }

    /// <summary>
    /// Fetches static platform profiling regulatory question blocks from cache streams.
    /// Note: This uses global static cache arrays and remains parameterless.
    /// </summary>
    [HttpGet("suitability/questions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuitabilityQuestions()
    {
        return Ok(await Mediator.Send(new GetSuitabilityQuestionsQuery()));
    }

    /// <summary>
    /// Resolves target alignment recommendations optimized for Shariah compliance bounds.
    /// </summary>
    [HttpGet("recommendations/target")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Bound to high-value subscription tracking criteria
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTargetRecommendations()
    {
        // 🛠️ FIXED: Passed required parameter context
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetTargetRecommendationsQuery(currentUserEmail)));
    }

    /// <summary>
    /// Extracts active risk audit boundaries and margin exposure details.
    /// </summary>
    [HttpGet("risk/audit-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRiskAuditSummary()
    {
        // 🛠️ FIXED: Passed required parameter context
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetRiskAuditSummaryQuery(currentUserEmail)));
    }

    /// <summary>
    /// Pulls accelerated high-frequency historical chart series matrices out of cache services.
    /// </summary>
    [HttpGet("market/live-charts")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Blocks unmonetized cache thread execution streams
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveChartsData()
    {
        // 🛠️ FIXED: Passed required parameter context
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);
        return Ok(await Mediator.Send(new GetLiveChartsDataQuery(currentUserEmail)));
    }

    /// <summary>
    /// Simulates compound growth trajectories over flexible asset timelines [CL-503].
    /// </summary>
    [HttpGet("portfolio/projections")]
    [SubscriptionRequirement("Automated_Exchange_Sync")] // 🔒 FEATURE GATE: Guards resource-heavy computational simulation paths
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ForecastProjectionsDto))]
    public async Task<IActionResult> GetForecastProjections([FromQuery] int horizonMonths)
    {
        // 🛠️ FIXED CS1503: Intercepts active claims to extract the non-null string email identifier
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🎯 CONTRACT REALIGNMENT: Correctly maps (string UserEmail, int ProjectionMonths) 
        // to match your explicit positional query record definition perfectly.
        var query = new GetForecastProjectionsQuery(
            UserEmail: currentUserEmail,
            ProjectionMonths: horizonMonths > 0 ? horizonMonths : 36
        );

        return Ok(await Mediator.Send(query));
    }
}