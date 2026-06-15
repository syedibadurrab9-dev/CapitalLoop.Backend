using Application.Chat.Commands.AcknowledgeChatMessage;
using Application.Chat.Commands.GetUnreadMessagesCount;
using Application.Chat.Commands.MarkMessagesAsRead;
using Application.Chat.Commands.SendChatMessage;
using Application.Common.Interfaces;
using Application.Portfolios.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers.Chat;

/// <summary>
/// 💬 REAL-TIME CHAT & CONSULTATION ROUTING GATEWAY
/// Orchestrates conversational threads, message acknowledgments, read states, and badge counters [CL-401].
/// </summary>
[Authorize]
public class ChatController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public ChatController(IIdentityService identityService)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
    }

    // =============================================================================
    // 🎛️ COMMAND ROUTING CHANNELS (STATE CHANGING OPERATIONS)
    // =============================================================================

    /// <summary>
    /// Transmits a new secure message payload down through the system pipeline threads.
    /// </summary>
    [HttpPost("messages/send")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    public async Task<IActionResult> SendChatMessage([FromBody] SendChatMessageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Dispatches the command safely down to its corresponding handler thread inside Application
        var messageId = await Mediator.Send(command);
        return Ok(messageId);
    }

    /// <summary>
    /// Updates message lifecycle tracks to acknowledge that a thread block has reached its destination target.
    /// </summary>
    [HttpPost("messages/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> AcknowledgeChatMessage([FromBody] AcknowledgeChatMessageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    /// <summary>
    /// Centralized bulk updating engine to flag all outstanding incoming thread nodes as read.
    /// </summary>
    [HttpPost("messages/mark-as-read")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    public async Task<IActionResult> MarkMessagesAsRead([FromBody] MarkMessagesAsReadCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Ok(await Mediator.Send(command));
    }

    // =============================================================================
    // 📈 QUERY ROUTING CHANNELS (IDEMPOTENT TELEMETRY FEEDS)
    // =============================================================================

    /// <summary>
    /// Pulls persistent chronological conversational history logs from storage layers between two identity nodes [CL-401].
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<ChatHistoryDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetChatHistory([FromQuery] string interactingWithEmail)
    {
        if (string.IsNullOrWhiteSpace(interactingWithEmail))
        {
            return BadRequest("Conversational lookup failed: The opposite target account email coordinate must be supplied.");
        }

        // 🎯 SECURITY ALIGNMENT: Resolves initiating identity properties straight from system claims
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        // 🛠️ FIXED CS1739: Maps (string CurrentEmail, string InteractingWithEmail) 
        // to match your exact positional record parameters flawlessly.
        var query = new GetChatHistoryQuery(
            CurrentEmail: currentUserEmail,
            InteractingWithEmail: interactingWithEmail.Trim().ToLowerInvariant()
        );

        return Ok(await Mediator.Send(query));
    }

    /// <summary>
    /// Extracts a high-speed scalar count of unread notifications to refresh UI badge components.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    public async Task<IActionResult> GetUnreadMessagesCount()
    {
        // 🎯 SECURITY ALIGNMENT: Resolves identity parameters to satisfy constructor contracts
        var currentUserEmail = _identityService.GetUserEmailFromPrincipal(User);

        return Ok(await Mediator.Send(new GetUnreadMessagesCountQuery(UserEmail: currentUserEmail)));
    }
}