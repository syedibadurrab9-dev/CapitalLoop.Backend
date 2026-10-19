using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class AssetNode : Entity
{
    public string UserEmail { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AssetCategory Category { get; private set; }
    public decimal HoldingValuePKR { get; private set; }
    public bool IsAutoSynced { get; private set; }

    // Telemetry identifiers for real estate installments
    public int? InstallmentsPaid { get; private set; }
    public int? TotalInstallments { get; private set; }
    public DateTime LastUpdatedUtc { get; private set; }

    private AssetNode() { }

    public static AssetNode CreateManual(string email, string name, AssetCategory category, decimal value, int? paid = null, int? total = null)
    {
        if (value <= 0)
            throw new InvalidAssetValueException("Asset valuation parameters must represent positive numeric sums.");

        if (category == AssetCategory.Real_Estate && paid > total)
            throw new InvalidInstallmentSchemaException("Completed payments cannot outpace your master real estate file framework.");

        return new AssetNode
        {
            Id = Guid.NewGuid(),
            UserEmail = email.ToLowerInvariant(),
            Name = name,
            Category = category,
            HoldingValuePKR = value,
            IsAutoSynced = false,
            InstallmentsPaid = category == AssetCategory.Real_Estate ? paid : null,
            TotalInstallments = category == AssetCategory.Real_Estate ? total : null,
            LastUpdatedUtc = DateTime.UtcNow
        };
    }

    public void HydrateLiveBalanceFromApi(decimal synchedValue)
    {
        HoldingValuePKR = synchedValue;
        IsAutoSynced = true;
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateInstallments(int paid, int total, decimal? updatedValuation = null)
    {
        if (Category != AssetCategory.Real_Estate)
            throw new InvalidOperationException("Installment telemetry updates apply exclusively to real estate asset nodes.");

        if (paid < 0 || total <= 0)
            throw new InvalidInstallmentSchemaException("Installment parameters must be positive integers.");

        if (paid > total)
            throw new InvalidInstallmentSchemaException("Completed payments cannot outpace your master real estate file framework.");

        InstallmentsPaid = paid;
        TotalInstallments = total;

        if (updatedValuation.HasValue)
        {
            if (updatedValuation.Value <= 0)
                throw new InvalidAssetValueException("Updated real estate valuation must be positive.");
            HoldingValuePKR = updatedValuation.Value;
        }

        LastUpdatedUtc = DateTime.UtcNow;
    }
}