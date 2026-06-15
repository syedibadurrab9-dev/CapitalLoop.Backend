using System;

namespace Application.Portfolios.Commands.QueueTaxAudit;

/// <summary>
/// 📨 HIGH-CONCURRENCY QUEUE TRANSACTION PAYLOAD
/// Lightweight, thread-safe data record passed through native memory channels
/// to execute background tax scraping and calculations [CL-501].
/// </summary>
public record TaxAuditQueueMessage(
    Guid JobId,
    string UserEmail,
    bool InitiatedBySystem
);