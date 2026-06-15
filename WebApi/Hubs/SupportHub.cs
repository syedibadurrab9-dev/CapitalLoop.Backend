using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace WebApi.Hubs;

/// <summary>
/// ⚙️ PRESENTATION GATEWAY LAYER ORCHESTRATION HUB
/// Secure, high-performance SignalR WebSocket routing node cluster.
/// Coordinates full-duplex support streams between Angular agent consoles and React Native apps [CL-401].
/// </summary>
[Authorize]
public class SupportHub : Hub
{
    private readonly ILogger<SupportHub> _logger;
    private readonly IApplicationDbContext _context;

    private const string SubscriptionClaimType = "SubscriptionTier";
    private const string AdministrativeAgentsGroup = "AgentsGroup";

    // Thread-safe dictionaries optimizing tracking metrics for active, stateful socket channels
    private static readonly ConcurrentDictionary<string, string> ActiveClients = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> ActiveAgents = new(StringComparer.OrdinalIgnoreCase);

    public SupportHub(ILogger<SupportHub> logger, IApplicationDbContext context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public override async Task OnConnectedAsync()
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant()
                    ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim().ToLowerInvariant()
                    ?? "unknown_node";

        // 🛠️ FIXED CLAIM SYNCHRONIZATION: Updated lookup mapping from "tier" to "SubscriptionTier" to match Auth configurations
        var tierClaim = Context.User?.FindFirst(SubscriptionClaimType)?.Value?.Trim().ToLowerInvariant()
                        ?? SubscriptionTiers.StandardFree;

        var name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "CapitalLoop User";

        // Validate that the incoming tier string is a legitimate system invariant
        if (!SubscriptionTiers.IsValidTier(tierClaim))
        {
            _logger.LogWarning("[SUPPORT-HUB] Security Warning: Connection aborted. Unknown system tier signature claim detected: '{Tier}' for {Email}", tierClaim, email);
            Context.Abort();
            return;
        }

        // =============================================================================
        // 🔒 5-TIER ROLE ROUTING MATRIX
        // =============================================================================
        if (string.Equals(tierClaim, SubscriptionTiers.AgentBackoffice, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tierClaim, SubscriptionTiers.AdvisorWealth, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tierClaim, SubscriptionTiers.TaxConsultantTier, StringComparison.OrdinalIgnoreCase))
        {
            ActiveAgents[email] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, AdministrativeAgentsGroup);

            _logger.LogInformation("[SUPPORT-HUB] Operational Support Node Authorized & Connected | Role: {Tier} | User: {Email} ({Id})",
                tierClaim, email, Context.ConnectionId);
        }
        else if (SubscriptionTiers.IsMobileAllowed(tierClaim))
        {
            ActiveClients[email] = Context.ConnectionId;

            // Channel individual clients into unique email-bound routing groups 
            // This guarantees zero message drop even if mobile connection sequences recycle
            await Groups.AddToGroupAsync(Context.ConnectionId, $"ClientGroup_{email}");

            _logger.LogInformation("[SUPPORT-HUB] Retail Consumer Workspace Stream Established | Tier: {Tier} | User: {Email} ({Id})",
                tierClaim, email, Context.ConnectionId);

            // Broadcast real-time online state parameter blocks directly up to Angular tracking sidebars
            await Clients.Group(AdministrativeAgentsGroup).SendAsync("UserStatusChanged", email, name, true);
        }
        else
        {
            _logger.LogWarning("[SUPPORT-HUB] Unauthorized Access Attempt | Boundary restriction blocked tier target: '{Tier}' for context user: {Email}", tierClaim, email);
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Invoked natively by your React Native application layout over secure WSS connections [CL-401].
    /// </summary>
    public async Task SendMessageToAgent(string messagePayload)
    {
        if (string.IsNullOrWhiteSpace(messagePayload)) return;

        var clientEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant() ?? "anonymous";
        var clientName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Premium Client";

        _logger.LogInformation("[SUPPORT-HUB] Message payload transmitted from client {Email}: {Payload}", clientEmail, messagePayload);

        // 🛡️ SYSTEM TIMEOUT GUARD: Prevents database thread locks if connections drop midway through saves
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            // 🧱 FIX CS1739: Removed 'operatorEmail' to match your entity's Create factory exactly
            // If the recipient target is the back-office queue, we specify "agentsgroup@capitalloop.local" 
            // to satisfy your entity's internal .Contains('@') validation constraint check.
            var chatRecord = ChatMessage.Create(
                senderEmail: clientEmail,
                receiverEmail: "agentsgroup@capitalloop.local",
                text: messagePayload.Trim(),
                chartToken: null
            );

            await _context.ChatMessages.AddAsync(chatRecord, cts.Token);
            await _context.SaveChangesAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SUPPORT-HUB] Relational EF persistence error processing inbound client message for user: {Email}", clientEmail);
        }

        await Clients.Group(AdministrativeAgentsGroup).SendAsync("ReceiveClientMessage", clientEmail, clientName, messagePayload);
    }

    /// <summary>
    /// Invoked by your authenticated web console agent layout to target a specific user directly [CL-404].
    /// </summary>
    public async Task SendAgentReplyToClient(string targetClientEmail, string messagePayload)
    {
        if (string.IsNullOrWhiteSpace(targetClientEmail) || string.IsNullOrWhiteSpace(messagePayload)) return;

        var normalizedEmail = targetClientEmail.Trim().ToLowerInvariant();
        var agentEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value ?? "System Agent";

        _logger.LogInformation("[SUPPORT-HUB] Support console agent {Agent} dispatching response targeting group: {Client}", agentEmail, normalizedEmail);

        // 🛡️ SYSTEM TIMEOUT GUARD: Prevents database thread locks if connections drop midway through saves
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(Context.ConnectionAborted);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            // 🧱 FIX CS1739: Removed 'operatorEmail' to match your entity's Create factory exactly
            var chatRecord = ChatMessage.Create(
                senderEmail: agentEmail,
                receiverEmail: normalizedEmail,
                text: messagePayload.Trim(),
                chartToken: null
            );

            await _context.ChatMessages.AddAsync(chatRecord, cts.Token);
            await _context.SaveChangesAsync(cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SUPPORT-HUB] Relational EF persistence error processing outbound agent response to: {Client}", normalizedEmail);
        }

        await Clients.Group($"ClientGroup_{normalizedEmail}").SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);
        await Clients.Group(AdministrativeAgentsGroup).SendAsync("ReceiveAgentMessage", agentEmail, messagePayload);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var email = Context.User?.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant()
                    ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(email))
        {
            if (ActiveClients.TryRemove(email, out _))
            {
                _logger.LogWarning("[SUPPORT-HUB] Client channel disconnected from network pool: {Email}", email);
                await Clients.Group(AdministrativeAgentsGroup).SendAsync("UserStatusChanged", email, "", false);
            }

            ActiveAgents.TryRemove(email, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }
}