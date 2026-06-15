using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Configures schema bounds, database constraints, search optimization indexes, 
/// and private collection backfields for the UserProfile aggregate root [CL-404].
/// </summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(p => p.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(p => p.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE BUSINESS DOMAIN ELEMENT ATTRIBUTES
        // =============================================================================
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(150);

        // ONSHORE PAKISTAN COMPLIANCE BOUNDARY: Fixed length configuration tracking for 13-digit CNIC format strings
        builder.Property(p => p.Cnic)
            .HasMaxLength(13)
            .IsFixedLength()
            .IsRequired(false);

        builder.Property(p => p.SubscriptionTier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Age)
            .IsRequired();

        builder.Property(p => p.IsFiler)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.KycStatus)
            .IsRequired()
            .HasConversion<string>() // Persists enum tags as strings for readability and data migration safety
            .HasMaxLength(30);

        builder.Property(p => p.RiskMatrix)
            .IsRequired()
            .HasConversion<string>() // Persists risk tolerance bands (Conservative/Balanced/Growth) as strings
            .HasMaxLength(30);

        builder.Property(p => p.SuitabilityAssessmentScore)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.SuitabilityAssessmentCompletedAtUtc)
            .IsRequired(false);

        // =============================================================================
        // 🔐 FOREIGN KEY DEPENDENCY MAPPING (1-TO-1 BOUNDARY)
        // =============================================================================
        builder.Property(p => p.ApplicationUserId)
              .IsRequired();

        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================================================
        // 🧬 ENCAPSULATION ENFORCEMENT VIA PROPERTY ACCESS MODES
        // =============================================================================
        // Directs EF Core to bind relational changes directly to the private _assetNodes backing field array
        builder.HasMany(p => p.AssetNodes)
            .WithOne()
            .HasForeignKey(a => a.UserEmail) // Relates child entries via email string contexts cleanly
            .HasPrincipalKey(p => p.Email)
            .Metadata.PrincipalToDependent?.SetPropertyAccessMode(PropertyAccessMode.Field);

        // =============================================================================
        // 🚀 HIGH-SPEED PERFORMANCE SEARCH INDEXES
        // =============================================================================
        builder.HasIndex(p => p.Email)
             .IsUnique()
             .HasDatabaseName("IX_UserProfiles_Email_Unique");

        builder.HasIndex(p => p.ApplicationUserId)
            .IsUnique()
            .HasDatabaseName("IX_UserProfiles_ApplicationUserId");

        // Multi-tenant optimization index to speed up backoffice KYC list checks
        builder.HasIndex(p => new { p.KycStatus, p.IsFiler })
            .HasDatabaseName("IX_UserProfiles_Compliance_Telemetry");
    }
}