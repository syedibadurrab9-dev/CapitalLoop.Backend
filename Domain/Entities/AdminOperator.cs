using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class AdminOperator : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public AdminRole Role { get; private set; }
    public string? LicenseNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime OnboardedUtc { get; private set; }
    public string OnboardedBy { get; private set; } = string.Empty;

    private AdminOperator() { }

    public static AdminOperator Provision(
        string name,
        string email,
        AdminRole role,
        string? licenseNumber,
        string onboardedBy)
    {
        return new AdminOperator
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim().ToUpperInvariant(),
            IsActive = true,
            OnboardedUtc = DateTime.UtcNow,
            OnboardedBy = onboardedBy.Trim().ToLowerInvariant()
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void UpdateRole(AdminRole role) => Role = role;
    public void UpdateLicense(string? licenseNumber) =>
        LicenseNumber = string.IsNullOrWhiteSpace(licenseNumber) ? null : licenseNumber.Trim().ToUpperInvariant();
}
