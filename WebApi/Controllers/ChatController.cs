using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApi.Hubs;

namespace WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/chat")]
public class ChatController : BaseApiController
{
    private readonly ISupportSessionManager _sessionManager;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ISupportSessionManager sessionManager, ILogger<ChatController> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Returns the persistent message history transcript for a specific client
    /// </summary>
    [HttpGet("history/{clientEmail}")]
    public IActionResult GetHistory(string clientEmail)
    {
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            return BadRequest(new { message = "Client email is required." });
        }

        var normalizedEmail = clientEmail.Trim().ToLowerInvariant();
        var history = _sessionManager.GetHistory(normalizedEmail);

        var viewModels = history.Select(h => new
        {
            messageId = h.MessageId,
            senderEmail = h.SenderEmail,
            senderName = h.SenderName,
            text = h.Text,
            isAgentSender = h.IsAgentSender,
            timestamp = h.Timestamp
        });

        return Ok(viewModels);
    }

    /// <summary>
    /// Returns all active and pending investor sessions
    /// </summary>
    [HttpGet("sessions")]
    public IActionResult GetActiveSessions()
    {
        var sessions = _sessionManager.GetAllSessions();
        return Ok(sessions);
    }

    /// <summary>
    /// Acknowledges / marks a client conversation thread as read
    /// </summary>
    [HttpPost("acknowledge/{clientEmail}")]
    public IActionResult AcknowledgeThread(string clientEmail)
    {
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            return BadRequest(new { message = "Client email is required." });
        }

        var normalizedEmail = clientEmail.Trim().ToLowerInvariant();
        _sessionManager.MarkThreadRead(normalizedEmail);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Claims an investor session for the authenticated advisor
    /// </summary>
    [HttpPost("claim/{clientEmail}")]
    public IActionResult ClaimSession(string clientEmail, [FromQuery] string? license, [FromQuery] bool force = false)
    {
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            return BadRequest(new { message = "Client email is required." });
        }

        var advisorEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "system_agent";
        var advisorName = User.FindFirst(ClaimTypes.Name)?.Value ?? "SECP Advisory Consultant";

        var result = _sessionManager.TryClaimSession(clientEmail, advisorEmail, advisorName, license, force);
        if (result.Success)
        {
            return Ok(new { success = true, session = result.Session });
        }

        return Conflict(new { success = false, message = result.Message, session = result.Session });
    }

    /// <summary>
    /// Releases an investor session back to the unassigned common pool
    /// </summary>
    [HttpPost("release/{clientEmail}")]
    public IActionResult ReleaseSession(string clientEmail)
    {
        if (string.IsNullOrWhiteSpace(clientEmail))
        {
            return BadRequest(new { message = "Client email is required." });
        }

        var advisorEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "system_agent";
        var result = _sessionManager.ReleaseSession(clientEmail, advisorEmail);

        return Ok(new { success = true, session = result.Session });
    }

    /// <summary>
    /// Returns closed / archived sessions for compliance audit logging
    /// </summary>
    [HttpGet("archive")]
    public IActionResult GetClosedSessionsArchive()
    {
        var sessions = _sessionManager.GetAllSessions()
            .Where(s => s.Status == "RESOLVED")
            .Select(s => new
            {
                sessionToken = s.InvestorEmail,
                clientEmail = s.InvestorEmail,
                clientName = s.InvestorName,
                openedAt = s.OpenedAt,
                isArchived = true,
                assignedAgentEmail = s.AssignedAdvisorEmail
            });

        return Ok(sessions);
    }
}
