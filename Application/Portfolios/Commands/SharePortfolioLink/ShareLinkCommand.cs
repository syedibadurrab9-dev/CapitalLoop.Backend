using System;
using MediatR;

namespace Application.Portfolios.Commands.SharePortfolioLink;

/// <summary>
/// CQRS Command to generate an insulated, read-only portfolio access tracking token [CL-209].
/// Allows authorized external reviewers (e.g., tax consultants) to view allocation breakdowns.
/// </summary>
/// <param name="UserEmail">The authenticated email address of the portfolio aggregate root owner.</param>
/// <param name="RecipientEmail">The targeted recipient email intended to consume the read-only breakdown.</param>
/// <param name="DurationDays">The operational lifespan window of the generated token string prior to auto-expiry (e.g., 7 days).</param>
public record SharePortfolioLinkCommand(
    string UserEmail,
    string RecipientEmail,
    int DurationDays = 7) : IRequest<SharedLinkResultDto>;