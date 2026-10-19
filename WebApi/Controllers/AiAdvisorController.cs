using Application.Common.Interfaces;
using Application.Portfolios.Commands.EvaluateSuitability;
using Application.Portfolios.Queries.GetRiskAuditSummary;
using Application.Portfolios.Queries.GetSuitabilityProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Authorize] // Protect auditing matrices via secure JWT verification filters
public class AiAdvisorController : BaseApiController
{
    private readonly IQueryHandler<GetRiskAuditSummaryQuery, RiskAuditDto> _riskAuditHandler;
    private readonly ICommandHandler<EvaluateSuitabilityCommand, SuitabilityProfileResultDto> _suitabilityHandler;
    private readonly IQueryHandler<GetSuitabilityProfileQuery, SuitabilityProfileResultDto> _profileQueryHandler;

    public AiAdvisorController(
        IQueryHandler<GetRiskAuditSummaryQuery, RiskAuditDto> riskAuditHandler,
        ICommandHandler<EvaluateSuitabilityCommand, SuitabilityProfileResultDto> suitabilityHandler,
        IQueryHandler<GetSuitabilityProfileQuery, SuitabilityProfileResultDto> profileQueryHandler)
    {
        _riskAuditHandler = riskAuditHandler;
        _suitabilityHandler = suitabilityHandler;
        _profileQueryHandler = profileQueryHandler;
    }

    /// <summary>
    /// Evaluates active portfolio nodes to flag over-concentration risk against sector ceilings.
    /// GET: api/AiAdvisor/audit-exposure?maxSectorCapPercentage=35.0
    /// </summary>
    [HttpGet("audit-exposure")]
    public async Task<IActionResult> AuditExposure([FromQuery] decimal? maxSectorCapPercentage, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        decimal targetCapCeiling = maxSectorCapPercentage ?? 30.0m;

        if (targetCapCeiling <= 0 || targetCapCeiling > 100)
        {
            return BadRequest(new { Message = "The sector distribution constraint ceiling parameter must sit between 0 and 100 percent." });
        }

        var validationAudit = await _riskAuditHandler.HandleAsync(
            new GetRiskAuditSummaryQuery(userEmail, targetCapCeiling),
            cancellationToken);

        return Ok(validationAudit);
    }

    /// <summary>
    /// Evaluates user's SECP suitability questionnaire answers, maps to risk profile, and enforces hard allocation caps.
    /// POST: api/AiAdvisor/suitability/evaluate
    /// </summary>
    [HttpPost("suitability/evaluate")]
    public async Task<IActionResult> EvaluateSuitability([FromBody] EvaluateSuitabilityRequest request, CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var command = new EvaluateSuitabilityCommand(userEmail, request.Answers);
        var result = await _suitabilityHandler.HandleAsync(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves user's registered SECP suitability profile, regulatory hard caps, and compliance state.
    /// GET: api/AiAdvisor/suitability/profile
    /// </summary>
    [HttpGet("suitability/profile")]
    public async Task<IActionResult> GetSuitabilityProfile(CancellationToken cancellationToken)
    {
        var userEmail = GetUserEmailFromClaims();
        var result = await _profileQueryHandler.HandleAsync(new GetSuitabilityProfileQuery(userEmail), cancellationToken);
        return Ok(result);
    }
}

public record EvaluateSuitabilityRequest(List<int> Answers);