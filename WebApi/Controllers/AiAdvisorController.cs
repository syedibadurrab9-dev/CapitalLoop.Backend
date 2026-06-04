using Application.Common.Interfaces; // Assuming IApplicationDbContext lives here
using Application.Portfolios.Queries.GetRiskAuditSummary;
using Infrastructure.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace WebApi.Controllers
{
    [Authorize] // Protect auditing matrices via secure JWT verification filters
    [ApiController]
    [Route("api/[controller]")]
    public class AiAdvisorController : BaseApiController
    {
        // Note: You can keep _context here if this controller needs to evaluate 
        // secondary profile states, otherwise MediatR isolates all DB data logic.
        private readonly IApplicationDbContext _context;

        public AiAdvisorController(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Evaluates active portfolio nodes to flag over-concentration risk against sector ceilings.
        /// GET: api/AiAdvisor/audit-exposure?maxSectorCapPercentage=35.0
        /// </summary>
        [HttpGet("audit-exposure")]
        public async Task<IActionResult> AuditExposure([FromQuery] decimal? maxSectorCapPercentage, CancellationToken cancellationToken)
        {
            // 1. Extract the secure, validated email string directly from the JWT claims token payload
            var userEmail = GetUserEmailFromClaims();

            // 2. Define a resilient threshold barrier fallback if the client leaves parameters unassigned
            decimal targetCapCeiling = maxSectorCapPercentage ?? 30.0m;

            if (targetCapCeiling <= 0 || targetCapCeiling > 100)
            {
                return BadRequest(new { Message = "The sector distribution constraint ceiling parameter must sit between 0 and 100 percent." });
            }

            // 3. Construct and dispatch the updated structural MediatR request pipeline contract
            var query = new GetRiskAuditSummaryQuery(userEmail, targetCapCeiling);

            var validationAudit = await Mediator.Send(query, cancellationToken);
            return Ok(validationAudit);
        }

        #region PRIVATE UTILITY METHODS

        /// <summary>
        /// Safely extracts the user's validated identity email context from incoming JWT authentication claims.
        /// </summary>
        private string GetUserEmailFromClaims()
        {
            var emailClaim = User.FindFirstValue(ClaimTypes.Email)
                             ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(emailClaim))
            {
                throw new UnauthorizedAccessException("The authenticated context contains an invalid or empty identity signature record.");
            }

            return emailClaim.ToLowerInvariant();
        }

        #endregion
    }
}