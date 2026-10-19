using Application.Common.Interfaces;
using Application.Portfolios.Commands.EvaluateSuitability;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetSuitabilityProfile;

public record GetSuitabilityProfileQuery(string UserEmail) : IQuery<SuitabilityProfileResultDto>;

public class GetSuitabilityProfileQueryHandler : IQueryHandler<GetSuitabilityProfileQuery, SuitabilityProfileResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetSuitabilityProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SuitabilityProfileResultDto> HandleAsync(GetSuitabilityProfileQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        var user = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var profile = user?.RiskProfile ?? "Balanced";
        var score = user?.SuitabilityScore ?? 12;

        var hardCaps = profile switch
        {
            "Conservative" => new Dictionary<string, decimal>
            {
                ["PSX_Equity"] = 20.0m,
                ["Crypto"] = 0.0m,
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
            _ => new Dictionary<string, decimal>
            {
                ["PSX_Equity"] = 80.0m,
                ["Crypto"] = 25.0m,
                ["Real_Estate"] = 50.0m,
                ["Precious_Metal"] = 15.0m,
                ["Mutual_Fund"] = 20.0m
            }
        };

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
                        violations.Add($"{group.Key} holding ({actualWeight}%) exceeds SECP {profile} hard ceiling ({cap}%)");
                    }
                }
            }
        }

        var isCompliant = violations.Count == 0;
        var remarks = isCompliant
            ? $"Portfolio conforms to SECP {profile} investor guidelines."
            : $"SECP ALLOCATION BREACH: {string.Join("; ", violations)}";

        return new SuitabilityProfileResultDto(
            RiskProfile: profile,
            SuitabilityScore: score,
            HardCaps: hardCaps,
            CurrentPortfolioCompliant: isCompliant,
            PolicyViolations: violations,
            AdvisoryRemarks: remarks,
            EvaluatedAtUtc: user?.SuitabilityEvaluatedUtc ?? DateTime.UtcNow
        );
    }
}
