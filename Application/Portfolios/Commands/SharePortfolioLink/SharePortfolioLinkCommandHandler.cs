using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.SharePortfolioLink;


/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Execution Handler generating secure access keys, binding expirations, and persisting share registry entries [CL-209].
/// Fully synchronized with the 'PortfolioShareLinks' contract table listed on your architectural matrix layout.
/// </summary>
public class SharePortfolioLinkCommandHandler : IRequestHandler<SharePortfolioLinkCommand, SharedLinkResultDto>
{
    private readonly IApplicationDbContext _context;

    public SharePortfolioLinkCommandHandler(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<SharedLinkResultDto> Handle(SharePortfolioLinkCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RecipientEmail))
            throw new ArgumentException("Share link generation aborted: Recipient destination email tracking coordinate cannot be null or empty.", nameof(command));

        var ownerEmail = command.UserEmail.Trim().ToLowerInvariant();
        var recipientEmail = command.RecipientEmail.Trim().ToLowerInvariant();

        // 1. Verify that the requesting portfolio context actually possesses active data properties
        var profileExists = await _context.UserProfiles
            .AnyAsync(p => p.Email == ownerEmail, cancellationToken);

        if (!profileExists)
            throw new InvalidOperationException($"Link Generation Aborted: Source user profile tracking index '{ownerEmail}' does not exist.");

        // 🔐 SECURE CRYPTOGRAPHIC TOKEN GENERATION
        // Leverages RNGCryptoServiceProvider layer calculations to guarantee high-entropy token strings, 
        // mitigating standard token predictability or iteration attacks.
        var secureRandomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secureRandomBytes);
        }
        string webSafeAccessToken = Convert.ToHexString(secureRandomBytes).ToLowerInvariant();

        // 2. Define structural timeframe parameters
        var expiresAtUtc = DateTime.UtcNow.AddDays(command.DurationDays);

        // 3. Instantiate the domain entity model tracker tracking the link sharing boundary
        // Directly references your exact master table mapping configuration.
        var shareLinkRegistry = new PortfolioShareLink(
            ownerEmail: ownerEmail,
            recipientEmail: recipientEmail,
            accessToken: webSafeAccessToken,
            expiresAtUtc: expiresAtUtc
        );

        // 🛠️ Mapped cleanly down across your synchronized context DbSet tracking property 'PortfolioShareLinks'
        await _context.PortfolioShareLinks.AddAsync(shareLinkRegistry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // 🚀 COMBINE OUTPUT ROUTING: Construct the final user-facing URI string
        string fullyQualifiedShareUrl = $"https://app.capitalloop.com/portfolio/shared/view?token={webSafeAccessToken}";

        return new SharedLinkResultDto(
            AccessToken: webSafeAccessToken,
            GeneratedShareUrl: fullyQualifiedShareUrl,
            RecipientEmailTarget: recipientEmail,
            ExpiresAtUtc: expiresAtUtc
        );
    }
}