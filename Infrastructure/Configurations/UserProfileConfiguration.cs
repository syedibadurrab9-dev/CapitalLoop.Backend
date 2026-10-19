using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.KycStatus).HasConversion<string>(); // Persist state as string tokens
        builder.Property(e => e.IsFiler).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.AtlVerifiedUtc);
        builder.Property(e => e.RiskProfile).HasMaxLength(50);
        builder.Property(e => e.SuitabilityScore);
        builder.Property(e => e.SuitabilityEvaluatedUtc);

        builder.HasIndex(e => e.Email).IsUnique();
    }
}
