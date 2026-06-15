using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Portfolios.Commands;

/// <summary>
/// ⚡ COMPLIANT CQRS EXECUTION HANDLER
/// Pulls active user profile roots, aggregates questionnaire scores, 
/// and executes encapsulated domain mutation rules natively [CL-301].
/// Fully synchronized with the vertical feature folder layout specifications.
/// </summary>
public class EvaluateSuitabilityCommandHandler : IRequestHandler<EvaluateSuitabilityCommand, SuitabilityResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EvaluateSuitabilityCommandHandler> _logger;

    public EvaluateSuitabilityCommandHandler(
        IApplicationDbContext context,
        ILogger<EvaluateSuitabilityCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SuitabilityResultDto> Handle(EvaluateSuitabilityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.QuestionResponses is null || !command.QuestionResponses.Any())
        {
            throw new ArgumentException("Suitability processing aborted: Questionnaire response dictionary tracking references cannot be null or empty.", nameof(command));
        }

        var sanitizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        _logger.LogInformation("Processing Suitability Assessment Evaluation | Profile Target: {Email} [CL-301]", sanitizedEmail);

        // 1. Fetch the targeted UserProfile record to modify its internal states
        var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.Email == sanitizedEmail, cancellationToken);

        if (userProfile is null)
        {
            throw new InvalidOperationException($"Suitability processing aborted: Profile tracking context could not be resolved for identity '{sanitizedEmail}'.");
        }

        // 2. Compute the total aggregate score from the questionnaire collection map values
        int aggregatedScore = command.QuestionResponses.Values.Sum();

        // 3. Map total score sums to standard regulatory risk tier descriptor strings
        string computedRiskLevel = "Conservative";

        if (aggregatedScore > 70)
        {
            computedRiskLevel = "Aggressive";
        }
        else if (aggregatedScore >= 30)
        {
            computedRiskLevel = "Moderate";
        }

        // 4. 🛡️ FIXED BUILD: Hand over the active operator email trace to satisfy your audit entity requirements
        userProfile.RecordSuitabilityAssessment(
            riskLevel: computedRiskLevel,
            calculationScore: aggregatedScore,
            operatorEmail: sanitizedEmail
        );

        // 5. Commit state alterations safely down to your underlying SQL database partition
        await _context.SaveChangesAsync(cancellationToken);

        // 6. Reflect the evaluated state back into the return payload context.
        // If your domain model downscaled an older investor to "Balanced" under age-capping policies, 
        // userProfile.RiskMatrix ensures your response matches the post-mutation truth.
        string finalAssignedTier = userProfile.RiskMatrix.ToString();

        _logger.LogInformation(
            "Suitability Assessment Committed Cleanly | Profile: {Email} | Initial Risk: {Initial} | Assigned Risk: {Final} | Accumulated Score: {Score}",
            sanitizedEmail, computedRiskLevel, finalAssignedTier, aggregatedScore);

        return new SuitabilityResultDto(
            AssignedRiskTier: finalAssignedTier,
            FinalCalculatedScore: aggregatedScore,
            AssessmentCompletedAtUtc: userProfile.SuitabilityAssessmentCompletedAtUtc ?? DateTime.UtcNow
        );
    }
}