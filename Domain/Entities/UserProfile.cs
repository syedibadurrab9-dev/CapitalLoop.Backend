using Domain.Common;
using Domain.Enums;
namespace Domain.Entities;

public class UserProfile : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsPremiumUser { get; private set; }
    public KycStatus KycStatus { get; private set; }
    public DateTime JoinedUtc { get; private set; }

    private UserProfile() { } // Required for ORM hydration

    public static UserProfile Register(string name, string email)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email.ToLowerInvariant(),
            IsPremiumUser = false,
            KycStatus = KycStatus.Unverified,
            JoinedUtc = DateTime.UtcNow
        };
    }

    public void UpgradeToPremium() => IsPremiumUser = true;
    public void RevokePremium() => IsPremiumUser = false;
    public void UpdateKycStatus(KycStatus status) => KycStatus = status;
}
