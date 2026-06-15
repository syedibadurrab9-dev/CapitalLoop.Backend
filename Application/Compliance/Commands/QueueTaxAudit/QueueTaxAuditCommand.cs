using System;
using MediatR;

namespace Application.Compliance.Commands.QueueTaxAudit;

/// <summary>
/// CQRS Command to initialize and queue an algorithmic tax optimization audit [CL-104].
/// Background workers or compliance scrapers process this token to map out withholding advantages.
/// </summary>
/// <param name="UserEmail">The authenticated email address of the portfolio workspace owner.</param>
/// <param name="IsTriggeredBySystem">True if an automated cron job or data scraper kicked off the request instead of a manual user click.</param>
/// <param name="InitiatedByEmail">The active identity trace of the operator or principal that executed this request block.</param>
public record QueueTaxAuditCommand(
    string UserEmail,
    bool IsTriggeredBySystem,
    string? InitiatedByEmail = null) : IRequest<Guid>;