using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Standardizes schema mappings, structural sizes, and bidirectional 1-to-1 boundaries 
/// for the updated ApplicationUser security domain entity [CL-403].
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(u => u.Id);



        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(u => u.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE IDENTITY SEED PROPERTIES & ALIGNMENTS
        // =============================================================================
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        /// <summary>
        /// 🔒 FEDERATED SSO LOOKUP ID: Decouples relational database Guid tracking keys 
        /// from volatile external alphanumeric security subject identifiers.
        /// </summary>
        builder.Property(u => u.IdentityProviderId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(u => u.SubscriptionTier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.SubscriptionUpdatedAtUtc)
            .IsRequired();

        // =============================================================================
        // 🚀 ECOSYSTEM RELATIONAL BRIDGE: 1-to-1 Principal Navigation mapping
        // =============================================================================
        // =============================================================================
        // 🚀 ECOSYSTEM RELATIONAL BRIDGE: 1-to-1 Principal Navigation mapping
        // =============================================================================
        // Explicitly map the relationship. 
        // ApplicationUser is the Principal, UserProfile is the Dependent.
        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Map the Optional FK property in ApplicationUser if it exists
        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<ApplicationUser>(u => u.UserProfileId)
            .IsRequired(false);

        // =============================================================================
        // 🧬 ENCAPSULATION ENFORCEMENT VIA PROPERTY ACCESS MODES
        // =============================================================================
        // 🧱 FIX CS1061: Steps down into the .Metadata layer to set the Field access mode safely
        builder.Navigation(u => u.Profile)
               .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);

        // =============================================================================
        // 🚀 HIGH-SPEED PERFORMANCE SEARCH INDEXES
        // =============================================================================
        builder.HasIndex(u => u.Email)
             .IsUnique()
             .HasDatabaseName("IX_Users_Email_Unique");

        builder.HasIndex(u => u.IdentityProviderId)
            .IsUnique()
            .HasDatabaseName("IX_Users_IdentityProviderId_Unique");
    }
}