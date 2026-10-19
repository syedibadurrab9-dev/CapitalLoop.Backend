using Application.Common.Interfaces;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Portfolios.Queries.GetForecastProjections;

public record AssetTaxBreakdownDto(
    string Category,
    decimal CurrentValuePKR,
    decimal PortfolioWeightPercentage,
    decimal NominalAnnualReturnRate,
    decimal FilerTaxRate,
    decimal NonFilerTaxRate,
    decimal EffectiveReturnRate
);

public record ForecastMilestoneDto(
    int Month,
    int Year,
    decimal ProjectedValuePKR,
    decimal NonFilerProjectedValuePKR,
    decimal TaxDragPKR,
    decimal CumulativeTaxSavedPKR
);

public record ForecastProjectionsResultDto(
    string UserEmail,
    bool IsFiler,
    DateTime? AtlVerifiedUtc,
    decimal CurrentTotalNetWorthPKR,
    decimal BlendedNominalAnnualReturn,
    decimal BlendedEffectiveAnnualReturn,
    List<AssetTaxBreakdownDto> AssetBreakdown,
    List<ForecastMilestoneDto> Milestones,
    decimal Forecast36MonthsPKR,
    decimal Forecast120MonthsPKR,
    decimal TaxSavings36MonthsPKR,
    decimal TaxSavings120MonthsPKR,
    string FbrAdvisoryNote
);

public record GetForecastProjectionsQuery(string UserEmail) : IQuery<ForecastProjectionsResultDto>;

public class GetForecastProjectionsQueryHandler : IQueryHandler<GetForecastProjectionsQuery, ForecastProjectionsResultDto>
{
    private readonly IApplicationDbContext _context;

    // Pakistani market statutory rates and baseline expected yields
    private static readonly Dictionary<string, (decimal NominalReturn, decimal FilerTax, decimal NonFilerTax)> AssetParameters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["PSX_Equity"] = (0.18m, 0.15m, 0.30m),     // 18% nominal yield; 15% CGT for Filers, 30% for Non-Filers
            ["Mutual_Fund"] = (0.15m, 0.15m, 0.30m),    // 15% nominal yield; 15% dividend tax for Filers, 30% for Non-Filers
            ["Real_Estate"] = (0.14m, 0.05m, 0.15m),    // 14% appreciation; 5% capital gain for Filers, 15% for Non-Filers
            ["FCY_Bank"] = (0.12m, 0.15m, 0.35m),       // 12% yield/deval; 15% bank profit tax for Filers, 35% for Non-Filers
            ["Precious_Metal"] = (0.16m, 0.05m, 0.10m), // 16% gold hedge; 5% capital gains for Filers, 10% for Non-Filers
            ["Crypto"] = (0.22m, 0.15m, 0.30m)          // 22% crypto beta; 15% capital gains for Filers, 30% for Non-Filers
        };

    public GetForecastProjectionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ForecastProjectionsResultDto> HandleAsync(GetForecastProjectionsQuery query, CancellationToken cancellationToken)
    {
        var normalizedEmail = query.UserEmail.Trim().ToLowerInvariant();

        var user = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var isFiler = user?.IsFiler ?? false;
        var atlVerifiedUtc = user?.AtlVerifiedUtc;

        var assets = await _context.Assets
            .AsNoTracking()
            .Where(a => a.UserEmail == normalizedEmail)
            .ToListAsync(cancellationToken);

        var totalNetWorthPKR = assets.Sum(a => a.HoldingValuePKR);

        // Group by category and compute breakdown
        var breakdown = new List<AssetTaxBreakdownDto>();
        decimal weightedNominalSum = 0m;
        decimal weightedEffectiveSum = 0m;
        decimal weightedNonFilerSum = 0m;
        decimal weightedFilerSum = 0m;

        if (totalNetWorthPKR > 0)
        {
            var grouped = assets.GroupBy(a => a.Category.ToString());
            foreach (var group in grouped)
            {
                var categoryKey = group.Key;
                var categoryVal = group.Sum(a => a.HoldingValuePKR);
                var weightPct = Math.Round((categoryVal / totalNetWorthPKR) * 100m, 2);
                var weightRatio = categoryVal / totalNetWorthPKR;

                var (nominalReturn, filerTax, nonFilerTax) = AssetParameters.TryGetValue(categoryKey, out var p)
                    ? p
                    : (0.12m, 0.15m, 0.30m); // Default conservative fallback

                var userTaxRate = isFiler ? filerTax : nonFilerTax;
                var effectiveReturn = Math.Round(nominalReturn * (1m - userTaxRate), 4);
                var filerEffective = nominalReturn * (1m - filerTax);
                var nonFilerEffective = nominalReturn * (1m - nonFilerTax);

                weightedNominalSum += weightRatio * nominalReturn;
                weightedEffectiveSum += weightRatio * effectiveReturn;
                weightedFilerSum += weightRatio * filerEffective;
                weightedNonFilerSum += weightRatio * nonFilerEffective;

                breakdown.Add(new AssetTaxBreakdownDto(
                    Category: categoryKey,
                    CurrentValuePKR: categoryVal,
                    PortfolioWeightPercentage: weightPct,
                    NominalAnnualReturnRate: nominalReturn,
                    FilerTaxRate: filerTax,
                    NonFilerTaxRate: nonFilerTax,
                    EffectiveReturnRate: effectiveReturn
                ));
            }
        }
        else
        {
            // Default baseline portfolio representation if portfolio is currently empty
            weightedNominalSum = 0.16m;
            weightedFilerSum = 0.16m * (1m - 0.15m);
            weightedNonFilerSum = 0.16m * (1m - 0.30m);
            weightedEffectiveSum = isFiler ? weightedFilerSum : weightedNonFilerSum;
        }

        var blendedNominal = Math.Round(weightedNominalSum, 4);
        var blendedEffective = Math.Round(weightedEffectiveSum, 4);
        var blendedFiler = Math.Round(weightedFilerSum, 4);
        var blendedNonFiler = Math.Round(weightedNonFilerSum, 4);

        // Project compounded wealth at discrete milestones (12, 24, 36, 60, 120 months)
        var milestoneMonths = new[] { 12, 24, 36, 60, 120 };
        var milestones = new List<ForecastMilestoneDto>();

        var principal = totalNetWorthPKR > 0 ? (double)totalNetWorthPKR : 1_000_000.0; // Default 1M baseline for illustration if empty

        foreach (var month in milestoneMonths)
        {
            var years = month / 12.0;

            // Compounding formula: A = P * (1 + r)^t
            var projectedVal = principal * Math.Pow(1.0 + (double)blendedEffective, years);
            var nonFilerProjectedVal = principal * Math.Pow(1.0 + (double)blendedNonFiler, years);
            var filerProjectedVal = principal * Math.Pow(1.0 + (double)blendedFiler, years);

            var taxDrag = filerProjectedVal - nonFilerProjectedVal;
            var cumulativeTaxSaved = isFiler ? taxDrag : 0.0;

            milestones.Add(new ForecastMilestoneDto(
                Month: month,
                Year: month / 12,
                ProjectedValuePKR: Math.Round((decimal)projectedVal, 2),
                NonFilerProjectedValuePKR: Math.Round((decimal)nonFilerProjectedVal, 2),
                TaxDragPKR: Math.Round((decimal)taxDrag, 2),
                CumulativeTaxSavedPKR: Math.Round((decimal)cumulativeTaxSaved, 2)
            ));
        }

        var m36 = milestones.First(m => m.Month == 36);
        var m120 = milestones.First(m => m.Month == 120);

        var advisoryNote = isFiler
            ? $"Verified FBR Active Taxpayer (ATL). By maintaining active filer status, your compounding portfolio avoids punitive non-filer withholding rates (up to 30%-35%), preserving an estimated {m36.TaxDragPKR:N0} PKR over 36 months and {m120.TaxDragPKR:N0} PKR over 120 months."
            : $"ACTION REQUIRED: Unverified or Non-Filer status detected on FBR ATL. Non-filers suffer up to double withholding tax (30% on PSX/Mutual funds, 35% on bank profits), causing an estimated compounding loss (tax drag) of {m36.TaxDragPKR:N0} PKR over 36 months and {m120.TaxDragPKR:N0} PKR over 120 months. File your tax return to reclaim active status.";

        return new ForecastProjectionsResultDto(
            UserEmail: normalizedEmail,
            IsFiler: isFiler,
            AtlVerifiedUtc: atlVerifiedUtc,
            CurrentTotalNetWorthPKR: totalNetWorthPKR,
            BlendedNominalAnnualReturn: blendedNominal,
            BlendedEffectiveAnnualReturn: blendedEffective,
            AssetBreakdown: breakdown,
            Milestones: milestones,
            Forecast36MonthsPKR: m36.ProjectedValuePKR,
            Forecast120MonthsPKR: m120.ProjectedValuePKR,
            TaxSavings36MonthsPKR: m36.TaxDragPKR,
            TaxSavings120MonthsPKR: m120.TaxDragPKR,
            FbrAdvisoryNote: advisoryNote
        );
    }
}
