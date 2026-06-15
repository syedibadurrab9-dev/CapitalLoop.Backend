using MediatR;

namespace Application.Compliance.Commands.SyncTaxFilerStatus;

/// <summary>
/// CQRS Command triggered by system scrapers or automated workers to sync an active FBR Active Taxpayer List status.
/// </summary>
/// <param name="UserEmail">The profile email identifier linked to the target CNIC being scraped.</param>
/// <param name="IsFiler">The active tax classification parameter verified against local revenue boards.</param>
public record SyncTaxFilerStatusCommand(string UserEmail, bool IsFiler) : IRequest<bool>;