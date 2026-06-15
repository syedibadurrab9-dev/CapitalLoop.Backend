using System;
using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Domain Entity Governing Full-Duplex Real-Time SignalR Support Chat Payloads [CL-401].
/// Encapsulates text transmission data streams, Base64 decentralized chart deep-link attachments,
/// and message read/acknowledgment visibility maps between retail users and enterprise advisor desks.
/// </summary>
public class ChatMessage : Entity
{
    // =============================================================================
    // 🧱 SECURE DOMAIN PROPERTY MATRIX (ENCAPSULATED STATE)
    // =============================================================================

    /// <summary>
    /// Gets the authenticated sender identifier email or systemic name tracking entity coordinates.
    /// </summary>
    public string SenderEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the targeted receiver email address anchoring conversational privacy matrices.
    /// </summary>
    public string ReceiverEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the plain text interactive conversational message content payload.
    /// </summary>
    public string MessageText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional Base64 compressed chart UI trajectory data string used for decentralized deep-linking sharing [CL-403].
    /// </summary>
    public string? ChartTokenPayload { get; private set; }

    /// <summary>
    /// Systemic workflow flag tracking whether a back-office agent has claimed or addressed this message in the queue.
    /// </summary>
    public bool IsAcknowledgedByOperator { get; private set; }

    /// <summary>
    /// End-user display visibility indicator checking if the targeted recipient has opened the transaction cell.
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// Required parameterless constructor for Entity Framework Core hydration mechanics.
    /// </summary>
    private ChatMessage() : base() { }

    // =============================================================================
    // 🚀 FACTORY INITIALIZATION PATTERNS
    // =============================================================================

    /// <summary>
    /// Factory initializer for persistent real-time SignalR thread messages [CL-401].
    /// Enforces rigorous data structure payload constraints prior to streaming ingestion.
    /// </summary>
    public static ChatMessage Create(string senderEmail, string receiverEmail, string text, string? chartToken = null)
    {
        if (string.IsNullOrWhiteSpace(senderEmail) || !senderEmail.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Message transmission sources must specify a valid authenticated email address structure.", nameof(senderEmail));

        if (string.IsNullOrWhiteSpace(receiverEmail) || !receiverEmail.Contains('@', StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Receiver identity coordinate is required and must follow standard email schemas.", nameof(receiverEmail));

        // Basic sanity check: Block cross-transmission back-loops
        if (senderEmail.Trim().Equals(receiverEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Conversational state failure: Message sender coordinates cannot mirror receiver destinations.");

        // 🛡️ SANITIZE STRINGS: Prevent empty space streams or broken data payloads from flooding storage capacity bounds
        var cleanedText = text?.Trim() ?? string.Empty;
        var cleanedToken = chartToken?.Trim();

        if (string.IsNullOrEmpty(cleanedText) && string.IsNullOrEmpty(cleanedToken))
            throw new ArgumentException("Dispatched interaction instances cannot transmit entirely empty payloads.");

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderEmail = senderEmail.Trim().ToLowerInvariant(),
            ReceiverEmail = receiverEmail.Trim().ToLowerInvariant(),
            MessageText = cleanedText,
            ChartTokenPayload = cleanedToken,
            IsAcknowledgedByOperator = false,
            IsRead = false
        };

        // Automate database validation audit footprints natively upon instance generation using client context
        message.SetCreationAuditTelemetry(message.SenderEmail);

        return message;
    }

    // =============================================================================
    // 🎛️ BULLETPROOF MUTATORS & VISIBILITY TRANSITIONS
    // =============================================================================

    /// <summary>
    /// Updates acknowledgment state variables when a back-office operator claims this message thread node [CL-404].
    /// </summary>
    /// <param name="operatorEmail">The email identity coordinates of the enterprise advisor modifying the record.</param>
    public void Acknowledge(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("State mutation tracking requires an authorized operator footprint.", nameof(operatorEmail));

        if (IsAcknowledgedByOperator) return; // Idempotency fallback gate safeguard

        IsAcknowledgedByOperator = true;

        // Push explicit security claims markers down into the entity audit trail tracking layer
        MarkAsModified(operatorEmail);
    }

    /// <summary>
    /// Synchronizes read-state flags and triggers infrastructure visibility updates [CL-501].
    /// </summary>
    /// <param name="operatorEmail">The email identity coordinates of the reading recipient principal context.</param>
    public void MarkAsRead(string operatorEmail)
    {
        if (string.IsNullOrWhiteSpace(operatorEmail))
            throw new ArgumentException("State mutation tracking requires an authorized operator footprint.", nameof(operatorEmail));

        if (IsRead) return; // Idempotency fallback gate safeguard

        IsRead = true;

        // Systemic cascade: If an active agent actively reads a message, auto-acknowledge processing threads safely
        if (!IsAcknowledgedByOperator)
        {
            IsAcknowledgedByOperator = true;
        }

        MarkAsModified(operatorEmail);
    }
}