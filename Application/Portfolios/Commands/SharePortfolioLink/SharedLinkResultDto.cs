using System;

namespace Application.Portfolios.Commands.SharePortfolioLink;

/// <summary>
/// Immutable return data transfer object wire record detailing the output of the link sharing workflow.
/// </summary>
public record SharedLinkResultDto(
    string AccessToken,
    string GeneratedShareUrl,
    string RecipientEmailTarget,
    DateTime ExpiresAtUtc
);