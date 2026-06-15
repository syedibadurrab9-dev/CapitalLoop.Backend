using System;

namespace Application.Portfolios.Queries;

/// <summary>
/// Strongly-typed, immutable record wire format representing an individual message block down on UI conversation logs.
/// </summary>
public record ChatHistoryDto(
    Guid MessageId,
    string SenderEmail,
    string ReceiverEmail,
    string MessageText,
    string? ChartTokenPayload,
    bool IsAcknowledgedByOperator,
    bool IsRead
);