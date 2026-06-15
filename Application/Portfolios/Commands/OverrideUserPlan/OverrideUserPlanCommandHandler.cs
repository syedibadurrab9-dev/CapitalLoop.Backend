using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.OverrideUserPlan;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Back-Office Execution Handler forcing authorization modifications, updating core table schemas, 
/// and evicting distributed user session memory cache allocations [CL-503].
/// </summary>
public class OverrideUserPlanCommandHandler : IRequestHandler<OverrideUserPlanCommand, SubscriptionOverrideResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ICacheService _cacheService;

    public OverrideUserPlanCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        ICacheService cacheService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<SubscriptionOverrideResultDto> Handle(OverrideUserPlanCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Payload);

        var emailTarget = command.Payload.TargetUserEmail.Trim().ToLowerInvariant();
        var desiredTier = command.Payload.RequestedTierToken.Trim();

        // 🎯 STEP 1: PERSISTENCE ALIGNMENT - Locate and mutate the domain UserProfile aggregate root state parameters
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.Email == emailTarget, cancellationToken);

        if (profile is null)
            throw new InvalidOperationException($"Administrative override failed: Target business user profile context '{emailTarget}' does not exist.");

        // =============================================================================
        // 🧱 FIXES CS1061: CAPTURE AND COMPOSE AUDIT TRACE FROM DTO PROPERTIES
        // =============================================================================
        // Packages your specific DTO justification text into an audit footprint signature string
        var administrativeAuditTrace = !string.IsNullOrWhiteSpace(command.Payload.AdministrativeJustificationReason)
            ? $"Admin Override Reason: {command.Payload.AdministrativeJustificationReason.Trim()}"
            : "Administrative adjustment executed via back-office console.";

        // Mutate the domain aggregate root state cleanly using both required arguments safely
        profile.UpdateSubscriptionPlan(desiredTier, administrativeAuditTrace);

        // 🎯 STEP 2: PLURALIZED PROPERTY ALIGNMENT - Locate and mutate identity records via your synchronized 'Users' property
        var identityUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == emailTarget, cancellationToken);

        if (identityUser is not null)
        {
            // Pass the custom justification string down to track identity state mutation signatures perfectly
            identityUser.ChangeSubscriptionTier(desiredTier, administrativeAuditTrace);
        }

        // Commit core structural domain modifications down to physical storage pools
        await _context.SaveChangesAsync(cancellationToken);

        // =============================================================================
        // 🧱 FIXES CS0019: RESOLVE GUID TYPE MISMATCH IN CLAIMS OVERRIDE
        // =============================================================================
        //var userIdString = identityUser != null
        //    ? identityUser.Id.ToString()
        //    : profile.ApplicationUserId;

        // Execute back-office token token overrides via UpdateUserClaimsAsync
        var overrideClaimsResult = await _identityService.UpdateUserClaimsAsync(profile.ApplicationUserId.ToString(), desiredTier);

        // 🎯 STEP 4: DISTRIBUTED CACHE PERFORMANCE PURGE - Instantly evict user validation cache allocations
        string userChartsCacheKey = $"charts:telemetry:{emailTarget}";
        await _cacheService.EvictAsync(userChartsCacheKey, cancellationToken);

        return new SubscriptionOverrideResultDto
        {
            IsOverrideExecutionSuccessful = overrideClaimsResult.IsOverrideExecutionSuccessful,
            UserEmailTarget = emailTarget,
            AssignedActiveTier = desiredTier,
            UpdatedSecurityJwtToken = overrideClaimsResult.UpdatedSecurityJwtToken,
            ProcessedTimestampUtc = DateTime.UtcNow
        };
    }
}