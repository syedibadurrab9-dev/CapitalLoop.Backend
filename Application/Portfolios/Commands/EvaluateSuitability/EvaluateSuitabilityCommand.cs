using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Commands.EvaluateSuitability;

public record SuitabilityProfileResultDto(
    string RiskProfile,
    int SuitabilityScore,
    Dictionary<string, decimal> HardCaps,
    bool CurrentPortfolioCompliant,
    List<string> PolicyViolations,
    string AdvisoryRemarks,
    DateTime EvaluatedAtUtc
);

public record EvaluateSuitabilityCommand(
    string UserEmail,
    List<int> Answers
) : ICommand<SuitabilityProfileResultDto>;

public class EvaluateSuitabilityCommandHandler : ICommandHandler<EvaluateSuitabilityCommand, SuitabilityProfileResultDto>
{
    private readonly IApplicationDbContext _context;

    public EvaluateSuitabilityCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SuitabilityProfileResultDto> HandleAsync(EvaluateSuitabilityCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        var user = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
            throw new KeyNotFoundException($"User profile for '{command.UserEmail}' was not found.");

        // 1. Calculate Score
        var totalScore = command.Answers != null && command.Answers.Any()
            ? command.Answers.Sum()
            : 10; // Default baseline score

        // 2. Map to SECP Risk Profile Category
        var riskProfile = totalScore switch
        {
            <= 8 => "Conservative",
            <= 13 => "Balanced",
            <= 18 => "Growth",
            _ => "Speculative"
        };

        // 3. SECP Regulatory Hard Allocation Caps (2026 Sandbox Rules)
        var hardCaps = riskProfile switch
        {
            "Conservative" => new Dictionary<string, decimal>
            {
                ["PSX_Equity"] = 20.0m,
                ["Crypto"] = 0.0m, // Strictly prohibited for conservative profiles under SECP
                ["Real_Estate"] = 20.0m,
                ["Precious_Metal"] = 30.0m,
                ["Mutual_Fund"] = 70.0m
            },
            "Balanced" => new Dictionary<string, decimal>
            {
                ["PSX_Equity"] = 40.0m,
                ["Crypto"] = 5.0m,
                ["Real_Estate"] = 30.0m,
                ["Precious_Metal"] = 25.0m,
                ["Mutual_Fund"] = 50.0m
            },
            "Growth" => new Dictionary<string, decimal>
            {
                ["PSX_Equity"] = 65.0m,
                ["Crypto"] = 15.0m,
                ["Real_Estate"] = 40.0m,
                ["Precious_Metal"] = 20.0m,
                ["Mutual_Fund"] = 30.0m
            },
            _ => new Dictionary<string, decimal> // Speculative
            {
                ["PSX_Equity"] = 80.0m,
                ["Crypto"] = 25.0m,
                ["Real_Estate"] = 50.0m,
                ["Precious_Metal"] = 15.0m,
                ["Mutual_Fund"] = 20.0m
            }
        };

        // 4. Audit Existing Portfolio Allocation Against Hard Caps
        var assets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == normalizedEmail)
            .ToListAsync(cancellationToken);

        var totalPortfolioValue = assets.Sum(a => a.HoldingValuePKR);
        var violations = new List<string>();

        if (totalPortfolioValue > 0)
        {
            var grouped = assets.GroupBy(a => a.Category.ToString());
            foreach (var group in grouped)
            {
                var categoryVal = group.Sum(a => a.HoldingValuePKR);
                var actualWeight = Math.Round((categoryVal / totalPortfolioValue) * 100m, 1);

                if (hardCaps.TryGetValue(group.Key, out var cap))
                {
                    if (actualWeight > cap)
                    {
                        violations.Add($"{group.Key} holding ({actualWeight}%) exceeds SECP {riskProfile} hard ceiling ({cap}%)");
                    }
                }
            }
        }

        var isCompliant = violations.Count == 0;
        var remarks = isCompliant
            ? $"Portfolio allocation conforms strictly to SECP {riskProfile} suitability guidelines."
            : $"SECP ALLOCATION BREACH: {string.Join("; ", violations)}. Automated rebalancing recommended.";

        // 5. Update Profile & Record Audit Trail
        user.UpdateSuitability(riskProfile, totalScore);

        var auditLog = ComplianceAuditLog.Create(
            targetEmail: normalizedEmail,
            reviewerAgentEmail: "secp-suitability-engine@capitalloop.pk",
            actionPerformed: "SECP_SUITABILITY_ASSESSMENT",
            comments: $"Assigned {riskProfile} profile (Score: {totalScore}/25). Compliant: {isCompliant}."
        );

        await _context.ComplianceAuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new SuitabilityProfileResultDto(
            RiskProfile: riskProfile,
            SuitabilityScore: totalScore,
            HardCaps: hardCaps,
            CurrentPortfolioCompliant: isCompliant,
            PolicyViolations: violations,
            AdvisoryRemarks: remarks,
            EvaluatedAtUtc: user.SuitabilityEvaluatedUtc ?? DateTime.UtcNow
        );
    }
}
