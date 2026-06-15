using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Commands;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Responsible for validating database profile invariants and utilizing domain factory patterns [CL-101].
/// Fully integrated with the synchronized database layer contracts.
/// </summary>
public class InitializeOnboardingCommandHandler : IRequestHandler<InitializeOnboardingCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InitializeOnboardingCommandHandler> _logger;

    public InitializeOnboardingCommandHandler(
        IApplicationDbContext context,
        ILogger<InitializeOnboardingCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(InitializeOnboardingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation("Processing Workspace Initialization | Target Identity: {Email} [CL-101]", sanitizedEmail);

        //// 🧱 FIX CS0019: Explicitly parse the incoming string ID into a structural Guid structure
        //if (!Guid.TryParse(command.ApplicationUserId, out var parsedUserGuid))
        //{
        //    _logger.LogError("Workspace Initialization Failed | Provided ApplicationUserId string '{UserId}' is not a valid cryptographic Guid formatting structure.", command.ApplicationUserId);
        //    throw new ArgumentException("Ecosystem mapping exception: The parent identity tracking link anchor parameter is structurally invalid.", nameof(command.ApplicationUserId));
        //}

        // 1. Verify that the parent security account exists inside the system context using parsed Guid types
        var identityUserExists = await _context.Users
            .AnyAsync(u => u.Id == command.ApplicationUserId, cancellationToken);

        if (!identityUserExists)
        {
            _logger.LogError("Workspace Initialization Failed | ApplicationUser Guid {UserId} was not found inside the secure identity registry.", command.ApplicationUserId);
            throw new InvalidOperationException("Ecosystem mapping exception: Cannot bootstrap profile fields for a non-existent security principal.");
        }

        // 2. Enforce strict profile uniqueness to prevent duplicating retail user profiles
        var profileExists = await _context.UserProfiles
            .AnyAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (profileExists)
        {
            _logger.LogWarning("Workspace Initialization Aborted | Profile registry already exists for target: {Email}", sanitizedEmail);
            return false;
        }

        // 3. Invoke your exact static domain factory pattern to instantiate the UserProfile block cleanly
        var newProfile = UserProfile.Register(
            name: command.FullName,
            email: sanitizedEmail
        );

        // 4. Assign the mandatory 1-to-1 relational security foreign key parameter explicitly
        // Note: We stringify it here to fit the existing property string field configuration on UserProfile
        newProfile.ApplicationUserId = command.ApplicationUserId;

        // 5. Stage and commit the entity transformation to the relational database
        await _context.UserProfiles.AddAsync(newProfile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Workspace Initialization Complete | Retail profile provisioned successfully via factory patterns for user: {Email}",
            sanitizedEmail);

        return true;
    }
}