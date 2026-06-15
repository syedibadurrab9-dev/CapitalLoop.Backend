using Application.Common.Interfaces;
using Application.Common.Interfaces.MarketIntelligence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Compliance.Commands.SyncTaxFilerStatus;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// High-speed synchronization handler querying active government databases and mapping outputs to profile aggregates [CL-603].
/// Fully integrated with the 'IFbrScraperService' interface contract specified in your master matrix.
/// </summary>
public class SyncTaxFilerStatusCommandHandler : IRequestHandler<SyncTaxFilerStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IFbrScraperService _fbrScraperService;
    private readonly ILogger<SyncTaxFilerStatusCommandHandler> _logger;

    public SyncTaxFilerStatusCommandHandler(
        IApplicationDbContext context,
        IFbrScraperService fbrScraperService,
        ILogger<SyncTaxFilerStatusCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _fbrScraperService = fbrScraperService ?? throw new ArgumentNullException(nameof(fbrScraperService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(SyncTaxFilerStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        // 1. Locate the core UserProfile domain aggregate root
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("Tax Filer Sync Short-Circuited | Targeted user profile context could not be found for identity '{Email}'.", sanitizedEmail);
            return false; // Silently short-circuit to protect scheduled automated background worker routines
        }

        // 🛡️ DOMAIN REGULATORY INVARIANT: Verify that the user possesses a registered CNIC before querying FBR portals
        if (string.IsNullOrWhiteSpace(profile.Cnic))
        {
            _logger.LogWarning("Tax Filer Sync Aborted | User profile '{Email}' does not possess an assigned, verified CNIC configuration asset.", sanitizedEmail);
            return false;
        }

        _logger.LogInformation("Launching Live FBR Scraper Request Tracking | Target: {Email}", sanitizedEmail);

        try
        {
            // 🎯 STEP 2: METRIC LIVE VERIFICATION - Query FBR endpoints directly using the matrix-aligned scraper method
            bool isCurrentlyRegisteredFiler = await _fbrScraperService.FetchActiveTaxFilerStatusAsync(profile.Cnic, cancellationToken);

            // =============================================================================
            // 🧱 RESOLVES CS7036: PASS COMPLIANCE TELEMETRY FOOTPRINT TO MUTATOR
            // =============================================================================
            // Assign an explicit system compliance engine tracking tag to trace database state mutations back cleanly
            const string executionOperator = "system-fbr-compliance-daemon@capitalloop.internal";

            // 🎯 STEP 3: STATE HYDRATION - Mutate aggregate tracking properties using both required arguments safely
            profile.SetFilerStatus(isCurrentlyRegisteredFiler, executionOperator);

            // 🎯 STEP 4: TRANSACTION PERSISTENCE - Commit updated filing status tracking values back down to storage
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tax Filer Sync Success | Identity '{Email}' status finalized to: IsFiler = {Status}", sanitizedEmail, isCurrentlyRegisteredFiler);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure encountered during live external FBR verification loop for profile index '{Email}'", sanitizedEmail);
            return false; // Absorb runtime pipeline breakdowns cleanly to insulate automated microservices loops
        }
    }
}