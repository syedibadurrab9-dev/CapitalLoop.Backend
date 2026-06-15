using Domain.Enums;
using FluentValidation;

namespace Application.Portfolios.Commands;

/// <summary>
/// Proactive validator ensuring asset ledger creations adhere to strict financial limits.
/// Intercepted concurrently by ValidationBehavior.cs to eliminate execution thread wastes [CL-102].
/// </summary>
public class CreateManualAssetNodeCommandValidator : AbstractValidator<CreateManualAssetNodeCommand>
{
    public CreateManualAssetNodeCommandValidator()
    {
        RuleFor(x => x.UserEmail)
            .NotEmpty().WithMessage("Asset position mapping requires an authenticated profile email.")
            .EmailAddress().WithMessage("Asset owner identification context must follow standard email schemas.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Asset position description moniker cannot be empty.")
            .MaximumLength(100).WithMessage("Asset description naming cannot exceed the maximum 100 character limit.");

        RuleFor(x => x.HoldingValuePkr)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Asset entry rejected: Initial portfolio positions valuation metrics cannot register negative numerical sums.");

        // 🛡️ PRECIOUS METALS SPECIFIC INVARIANT SAFEGUARDS
        When(x => x.Category == AssetCategory.PreciousMetal, () =>
        {
            RuleFor(x => x.TotalTolas)
                .NotNull().WithMessage("Precious metal positions require an explicit weight specification in Tolas.")
                .GreaterThan(0).WithMessage("Commodity weight properties must represent a positive non-zero quantity.")
                .LessThanOrEqualTo(500).WithMessage("Ecosystem Protection Cap: Individual physical asset storage tracking is capped at a maximum of 500 Tolas.");

            RuleFor(x => x.CostPerTola)
                .NotNull().WithMessage("Precious metal ledger tracking requires an acquisition cost parameter per Tola.")
                .GreaterThan(0).WithMessage("Acquisition cost properties must represent a positive non-zero metric.");
        });

        // 🛡️ REAL ESTATE SPECIFIC INVARIANT SAFEGUARDS
        When(x => x.Category == AssetCategory.RealEstate, () =>
        {
            RuleFor(x => x.TotalInstallments)
                .GreaterThan(0).When(x => x.TotalInstallments.HasValue)
                .WithMessage("Master real estate configuration structures must reflect a positive baseline installment count.");

            RuleFor(x => x.InstallmentsPaid)
                .LessThanOrEqualTo(x => x.TotalInstallments)
                .When(x => x.InstallmentsPaid.HasValue && x.TotalInstallments.HasValue)
                .WithMessage("Completed payment sequences cannot outpace your master real estate file framework.");

            RuleFor(x => x.InstallmentsPaid)
                .GreaterThanOrEqualTo(0).When(x => x.InstallmentsPaid.HasValue)
                .WithMessage("Completed installment payment metrics cannot reflect negative parameters.");
        });
    }
}