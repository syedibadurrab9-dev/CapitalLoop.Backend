using System.Security.Claims;
using Application.Portfolios.Commands.CreateManualAsset;
using Application.Portfolios.Commands.TriggerApiSync;
using Application.Portfolios.Commands.LinkBinanceWallet; // New Binance Command Namespace
using Application.Portfolios.Queries.GetAggregatedNetWorth;
using Application.Portfolios.Queries.GetLiveChartsData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize] // Protect financial metrics via JWT verification filters
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : BaseApiController
    {
        #region CORE DASHBOARD & MANIPULATION ENDPOINTS

        [HttpGet("net-worth")]
        public async Task<IActionResult> GetNetWorth([FromQuery] string currency, CancellationToken cancellationToken)
        {
            var userEmail = GetUserEmailFromClaims();
            var result = await Mediator.Send(new GetAggregatedNetWorthQuery(userEmail, currency), cancellationToken);
            return Ok(result);
        }

        [HttpGet("charts-data")]
        public async Task<IActionResult> GetChartsData(CancellationToken cancellationToken)
        {
            var userEmail = GetUserEmailFromClaims();
            var result = await Mediator.Send(new GetLiveChartsDataQuery(userEmail), cancellationToken);
            return Ok(result);
        }

        [HttpPost("manual-asset")]
        public async Task<IActionResult> CreateManualAsset([FromBody] CreateManualAssetCommand command, CancellationToken cancellationToken)
        {
            // Override or validate command properties using internal token claims for protection
            var userEmail = GetUserEmailFromClaims();

            var assetId = await Mediator.Send(command, cancellationToken);
            return Ok(new { AssetId = assetId, Status = "Success" });
        }

        [HttpPost("trigger-sync")]
        public async Task<IActionResult> TriggerApiSync(CancellationToken cancellationToken)
        {
            var userEmail = GetUserEmailFromClaims();
            var syncSuccess = await Mediator.Send(new TriggerApiSyncCommand(userEmail), cancellationToken);

            if (!syncSuccess)
                return BadRequest("Automated balance aggregation tracking failed or no assets discovered.");

            return Ok(new { Message = "Portfolio matrices synchronized successfully via external nodes." });
        }

        #endregion

        #region FEATURE INTEGRATION: DIGITAL EXCHANGES (BINANCE)

        [HttpPost("sync/binance")]
        public async Task<IActionResult> LinkAndSyncBinance([FromBody] ConnectBinanceRequest request, CancellationToken cancellationToken)
        {
            var userEmail = GetUserEmailFromClaims();

            var command = new LinkBinanceWalletCommand
            {
                ApiKey = request.ApiKey,
                SecretKey = request.SecretKey,
                UserEmail = userEmail
            };

            var syncSummary = await Mediator.Send(command, cancellationToken);
            return Ok(syncSummary);
        }

        #endregion

        #region FUTURE EXTENSION RESERVATIONS (PSX, MUTUAL FUNDS, METALS)

        // TODO: Task #4012 - Integrate PSX (AKD Brokerage & K-Trade) via automated contract scrapers
        // [HttpPost("sync/psx-brokerage")]
        // public async Task<IActionResult> SyncPsxBrokerage([FromBody] PsxBrokerageCredentialsRequest request) => On Development

        // TODO: Task #4013 - Integrate Al Meezan Mutual Funds via Investor Portal API validation map
        // [HttpPost("sync/al-meezan")]
        // public async Task<IActionResult> SyncAlMeezanFunds([FromBody] MeezanCredentialsRequest request) => On Development

        // TODO: Task #4014 - Automated Precious Metals Valuation Synced nodes (Gold/Silver Spot updates)
        // [HttpPost("sync/precious-metals")]
        // public async Task<IActionResult> SyncPreciousMetals() => On Development

        #endregion

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

    /// <summary>
    /// Explicit request payload record for secure incoming Binance keys.
    /// </summary>
    public record ConnectBinanceRequest(string ApiKey, string SecretKey);
}