using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace WebApi.Hubs;

[Authorize]
public class SupportHub : Hub
{
    private readonly ILogger<SupportHub> _logger;

    // Case-insensitive dictionaries tracking active communication routes across the hub footprint
    private static readonly ConcurrentDictionary<string, string> ActiveClients = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> ActiveAgents = new(StringComparer.OrdinalIgnoreCase);

    public SupportHub(ILogger<SupportHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant()
                    ?? Context.User?.Identity?.Name?.Trim().ToLowerInvariant()
                    ?? "unknown_node";

        var tierClaim = Context.User?.FindFirst("tier")?.Value?.Trim().ToLowerInvariant() ?? "free";
        var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "CapitalLoop User";

        // ✅ FIX: Explicitly direct users carrying the 'agent' tier claim to the Agent stack
        if (tierClaim == "agent")
        {
            ActiveAgents[email] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, "AgentsGroup");
            _logger.LogInformation("🎧 Support Agent authorized & connected: {Email} ({Id})", email, Context.ConnectionId);
        }
        else
        {
            ActiveClients[email] = Context.ConnectionId;

            // Channel individual clients into unique email-bound sub-groups 
            // This guarantees delivery even if their mobile socket connection recycles
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ClientGroup_{email}");
            _logger.LogInformation("🚀 Client connected to support channel: {Email} ({Id}) [Tier: {Tier}]", email, Context.ConnectionId, tierClaim);

            // Broadcast real-time online parameter blocks straight to your Angular panel sidebar layout
            await Clients.Group("AgentsGroup").SendAsync("UserStatusChanged", email, name, true);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Invoked natively by your React Native app over secure WSS links
    /// </summary>
    public async Task SendMessageToAgent(string messagePayload)
    {
        var clientEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "anonymous";
        var clientName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Premium Client";

        _logger.LogInformation("✉️ Message received from Client {Email}: {Payload}", clientEmail, messagePayload);

        // Forward message payload directly to the administrative workspace listening inside AgentsGroup
        await Clients.Group("AgentsGroup").SendAsync("ReceiveClientMessage", clientEmail, clientName, messagePayload);
    }

    /// <summary>
    /// Invoked by your authenticated web console agent to target a specific user directly
    /// </summary>
    public async Task SendAgentReplyToClient(string targetClientEmail, string messagePayload)
    {
        var normalizedEmail = targetClientEmail.Trim().ToLowerInvariant();
        var agentEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "System Agent";

        _logger.LogInformation("✉️ Agent {Agent} responding explicitly to client group: {Client}", agentEmail, normalizedEmail);

        // ✅ FIX: Broadcast directly to the user's dedicated group rather than a volatile single connection ID
        await Clients.Group($"ClientGroup_{normalizedEmail}").SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);

        // Echo the message back to the active AgentsGroup so all logged-in agent portal views stay synced
        await Clients.Group("AgentsGroup").SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant()
                    ?? Context.User?.Identity?.Name?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(email))
        {
            if (ActiveClients.TryRemove(email, out _))
            {
                _logger.LogWarning("❌ Client disconnected: {Email}", email);
                await Clients.Group("AgentsGroup").SendAsync("UserStatusChanged", email, "", false);
            }

            ActiveAgents.TryRemove(email, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }
}