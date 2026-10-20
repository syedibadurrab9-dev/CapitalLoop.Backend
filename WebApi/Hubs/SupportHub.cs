using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace WebApi.Hubs;

[Authorize]
public class SupportHub : Hub
{
    private readonly ILogger<SupportHub> _logger;
    private readonly ISupportSessionManager _sessionManager;

    public SupportHub(ILogger<SupportHub> logger, ISupportSessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant()
                    ?? Context.User?.Identity?.Name?.Trim().ToLowerInvariant()
                    ?? "unknown_node";

        var tierClaim = Context.User?.FindFirst("tier")?.Value?.Trim().ToLowerInvariant() ?? "free";
        var roleClaim = Context.User?.FindFirst(ClaimTypes.Role)?.Value?.Trim().ToLowerInvariant() ?? "";
        var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "CapitalLoop User";

        bool isAdvisor = tierClaim == "agent" || roleClaim == "admin" || roleClaim == "superadmin";

        // Register unique connectionId in the session manager without overwriting other active connections
        _sessionManager.AddConnection(Context.ConnectionId, email, name, isAdvisor);

        if (isAdvisor)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AgentsGroup");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"AdvisorGroup_{email}");
            _logger.LogInformation("🎧 Advisor connected: {Email} (ConnId: {Id})", email, Context.ConnectionId);

            // Send current list of active sessions with their engagement states directly to this newly connected advisor
            var sessions = _sessionManager.GetAllSessions();
            await Clients.Caller.SendAsync("ActiveSessionsLoaded", sessions);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ClientGroup_{email}");
            _logger.LogInformation("🚀 Investor connected: {Email} (ConnId: {Id}) [Tier: {Tier}]", email, Context.ConnectionId, tierClaim);

            // Notify advisors in AgentsGroup that investor status is online
            await Clients.Group("AgentsGroup").SendAsync("UserStatusChanged", email, name, true);

            // If this investor's session is already engaged with an advisor, notify this connection immediately
            var session = _sessionManager.GetSession(email);
            if (session != null && session.Status == "ENGAGED" && !string.IsNullOrEmpty(session.AssignedAdvisorName))
            {
                await Clients.Caller.SendAsync("AdvisorAssigned", session.AssignedAdvisorEmail, session.AssignedAdvisorName, session.AssignedAdvisorLicense);
            }
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Invoked natively by your React Native app when the investor sends a message
    /// </summary>
    public async Task SendMessageToAgent(string messagePayload)
    {
        var clientEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "anonymous";
        var clientName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Premium Client";

        _logger.LogInformation("✉️ Message received from Client {Email}: {Payload}", clientEmail, messagePayload);

        // Store message in persistent session history
        var msg = _sessionManager.AddMessage(clientEmail, clientEmail, clientName, messagePayload, isAgentSender: false);

        // Forward message to all advisors on AgentsGroup
        await Clients.Group("AgentsGroup").SendAsync("ReceiveClientMessage", clientEmail, clientName, messagePayload);

        // If this investor is actively engaged with a specific advisor, push high-priority event to that advisor
        var session = _sessionManager.GetSession(clientEmail);
        if (session != null && session.Status == "ENGAGED" && !string.IsNullOrEmpty(session.AssignedAdvisorEmail))
        {
            await Clients.Group($"AdvisorGroup_{session.AssignedAdvisorEmail}").SendAsync(
                "ReceiveAssignedClientMessage", clientEmail, clientName, messagePayload);
        }
    }

    /// <summary>
    /// Invoked by an advisor from the Angular portal to send an official response to an investor
    /// </summary>
    public async Task SendAgentReplyToClient(string targetClientEmail, string messagePayload, string? consultantLicense = null)
    {
        var normalizedClient = targetClientEmail.Trim().ToLowerInvariant();
        var agentEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "system_agent";
        var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "SECP Advisory Consultant";

        var session = _sessionManager.GetSession(normalizedClient);

        // Check single-advisor engagement locking:
        // If session is already engaged by another advisor, prevent cross-advisor collision!
        if (session != null && session.Status == "ENGAGED" &&
            !string.IsNullOrEmpty(session.AssignedAdvisorEmail) &&
            !string.Equals(session.AssignedAdvisorEmail, agentEmail, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("⚠️ Collision avoided: Advisor {Agent} attempted to message client {Client} already locked to {Assigned}",
                agentEmail, normalizedClient, session.AssignedAdvisorEmail);

            await Clients.Caller.SendAsync("EngagementWarning", 
                $"Investor is currently engaged with {session.AssignedAdvisorName} ({session.AssignedAdvisorEmail}). Take over the session to reply.");
            return;
        }

        // If session is unassigned, automatically claim it for this responding advisor
        if (session == null || session.Status != "ENGAGED")
        {
            var claimResult = _sessionManager.TryClaimSession(normalizedClient, agentEmail, agentName, consultantLicense);
            if (claimResult.Success)
            {
                await Clients.Group("AgentsGroup").SendAsync("InvestorEngagementChanged", claimResult.Session);
                await Clients.Group($"ClientGroup_{normalizedClient}").SendAsync(
                    "AdvisorAssigned", agentEmail, agentName, claimResult.Session.AssignedAdvisorLicense);
            }
        }

        _logger.LogInformation("✉️ Agent {Agent} ({Name}) replying to client {Client}", agentEmail, agentName, normalizedClient);

        // Store message in persistent session history
        var msg = _sessionManager.AddMessage(normalizedClient, agentEmail, agentName, messagePayload, isAgentSender: true);

        // Send directly to the investor's connection group
        await Clients.Group($"ClientGroup_{normalizedClient}").SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);

        // Echo to all advisors so portal conversation views remain synchronized
        await Clients.Group("AgentsGroup").SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);
        await Clients.Group("AgentsGroup").SendAsync("ChatUpdated", normalizedClient);
    }

    /// <summary>
    /// Explicitly locks/claims an investor session to the calling advisor
    /// </summary>
    public async Task<object> ClaimInvestorSession(string targetClientEmail, string? consultantLicense = null, bool forceTakeover = false)
    {
        var normalizedClient = targetClientEmail.Trim().ToLowerInvariant();
        var agentEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "system_agent";
        var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "SECP Advisory Consultant";

        var result = _sessionManager.TryClaimSession(normalizedClient, agentEmail, agentName, consultantLicense, forceTakeover);

        if (result.Success)
        {
            _logger.LogInformation("🔒 Investor {Client} claimed by Advisor {Agent} ({Name})", normalizedClient, agentEmail, agentName);

            // Broadcast engagement update to all advisors in AgentsGroup
            await Clients.Group("AgentsGroup").SendAsync("InvestorEngagementChanged", result.Session);

            // Notify investor on mobile that their dedicated advisor has joined
            await Clients.Group($"ClientGroup_{normalizedClient}").SendAsync(
                "AdvisorAssigned", agentEmail, agentName, result.Session.AssignedAdvisorLicense);

            return new { success = true, session = result.Session };
        }
        else
        {
            _logger.LogWarning("❌ Failed to claim investor {Client}: {Msg}", normalizedClient, result.Message);
            return new { success = false, message = result.Message, session = result.Session };
        }
    }

    /// <summary>
    /// Releases an investor session back to the unassigned common pool
    /// </summary>
    public async Task<object> ReleaseInvestorSession(string targetClientEmail)
    {
        var normalizedClient = targetClientEmail.Trim().ToLowerInvariant();
        var agentEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "system_agent";

        var result = _sessionManager.ReleaseSession(normalizedClient, agentEmail);

        if (result.Success)
        {
            _logger.LogInformation("🔓 Investor {Client} released back to pool by Advisor {Agent}", normalizedClient, agentEmail);

            // Broadcast engagement release to all advisors in AgentsGroup
            await Clients.Group("AgentsGroup").SendAsync("InvestorEngagementChanged", result.Session);

            // Notify mobile investor that session is in pool
            await Clients.Group($"ClientGroup_{normalizedClient}").SendAsync("AdvisorReleased");

            return new { success = true, session = result.Session };
        }

        return new { success = false, message = result.Message };
    }

    /// <summary>
    /// Fetches all active sessions with engagement states for the calling advisor
    /// </summary>
    public Task<IReadOnlyCollection<InvestorChatSession>> GetActiveSessions()
    {
        return Task.FromResult(_sessionManager.GetAllSessions());
    }

    /// <summary>
    /// Sends real-time typing indicators between counterparts
    /// </summary>
    public async Task SendTypingNotification(string targetEmail, bool isTyping)
    {
        var senderEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "unknown";
        var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "User";
        var normalizedTarget = targetEmail.Trim().ToLowerInvariant();

        await Clients.Group($"ClientGroup_{normalizedTarget}").SendAsync("UserTyping", senderEmail, senderName, isTyping);
        await Clients.Group($"AdvisorGroup_{normalizedTarget}").SendAsync("UserTyping", senderEmail, senderName, isTyping);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var disconnectInfo = _sessionManager.RemoveConnection(Context.ConnectionId);

        if (disconnectInfo.IsCompletelyDisconnected && !string.IsNullOrEmpty(disconnectInfo.Email))
        {
            if (!disconnectInfo.IsAdvisor)
            {
                _logger.LogWarning("❌ Investor fully disconnected (0 active sockets): {Email}", disconnectInfo.Email);
                await Clients.Group("AgentsGroup").SendAsync("UserStatusChanged", disconnectInfo.Email, disconnectInfo.Name, false);
            }
            else
            {
                _logger.LogInformation("🎧 Advisor disconnected (0 active sockets): {Email}", disconnectInfo.Email);
            }
        }
        else
        {
            _logger.LogInformation("🔌 Single socket connection dropped for {Email} ({Id}), but other active sockets remain.",
                disconnectInfo.Email, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
