using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 RELATIONAL SCHEMA CONFIGURATION MATRIX [CL-209]
/// Establishes Fluent API table schemas, constraint criteria, structural string sizes,
/// and lookup optimization indexes for the PortfolioShareLink aggregate model.
/// </summary>
public class PortfolioShareLinkConfiguration : IEntityTypeConfiguration<PortfolioShareLink>
{
    public void Configure(EntityTypeBuilder<PortfolioShareLink> builder)
    {
        // 🎯 STEP 1: TABLE MAPPING DEFINITIONS
        builder.ToTable("PortfolioShareLinks");

        // 🎯 STEP 2: PRIMARY KEY MAPPING (Inherited from Base Entity)
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever(); // Guided explicitly via high-entropy System.Guid initialization loops

        // 🎯 STEP 3: STRICT FIELD CAPACITY CONFIGURATIONS
        builder.Property(x => x.OwnerEmail)
            .IsRequired()
            .HasMaxLength(256); // Sized to match enterprise SMTP addressing protocols

        builder.Property(x => x.RecipientEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.AccessToken)
            .IsRequired()
            .HasMaxLength(512); // Extends adequate buffer spacing for high-entropy cryptographically signed token variants

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        // 🎯 STEP 4: IGNORE COMPUTED DOMAIN LOGIC PROPERTIES
        builder.Ignore(x => x.IsActive);

        // =============================================================================
        // ⚡ HIGH-SPEED PERFORMANCE & INTEGRITY INDEX MATRIX
        // =============================================================================

        // 1. Optimizes lookup sweeps for workspace owners auditing their active shared channel pipes
        builder.HasIndex(x => x.OwnerEmail)
            .HasDatabaseName("IX_PortfolioShareLinks_OwnerEmail");

        // 2. Optimizes downstream access handshakes made via secure incoming invite routes
        builder.HasIndex(x => x.RecipientEmail)
            .HasDatabaseName("IX_PortfolioShareLinks_RecipientEmail");

        // 3. Unique performance covering index over the access token hash string to ensure maximum handshake velocity
        builder.HasIndex(x => x.AccessToken)
            .IsUnique()
            .HasDatabaseName("UX_PortfolioShareLinks_AccessToken");
    }
}