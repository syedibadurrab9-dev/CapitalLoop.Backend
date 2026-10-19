using Domain.Common;
using Domain.Enums;
namespace Domain.Entities;

public class UserProfile : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsPremiumUser { get; private set; }
    public KycStatus KycStatus { get; private set; }
    public bool IsFiler { get; private set; }
    public DateTime? AtlVerifiedUtc { get; private set; }
    public string? RiskProfile { get; private set; }
    public int? SuitabilityScore { get; private set; }
    public DateTime? SuitabilityEvaluatedUtc { get; private set; }
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
            IsFiler = false,
            AtlVerifiedUtc = null,
            RiskProfile = null,
            SuitabilityScore = null,
            SuitabilityEvaluatedUtc = null,
            JoinedUtc = DateTime.UtcNow
        };
    }

    public void UpgradeToPremium() => IsPremiumUser = true;
    public void RevokePremium() => IsPremiumUser = false;
    public void UpdateKycStatus(KycStatus status) => KycStatus = status;

    public void UpdateFilerStatus(bool isFiler)
    {
        IsFiler = isFiler;
        AtlVerifiedUtc = DateTime.UtcNow;
    }

    public void UpdateSuitability(string riskProfile, int score)
    {
        RiskProfile = riskProfile;
        SuitabilityScore = score;
        SuitabilityEvaluatedUtc = DateTime.UtcNow;
    }
}
