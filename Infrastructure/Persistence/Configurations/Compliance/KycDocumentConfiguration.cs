using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties, split asset paths, and strict 13-digit identity constraints 
/// for the updated KycDocument domain entity [CL-408 / CL-601].
/// </summary>
public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(k => k.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(k => k.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(k => k.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE REGULATORY COMPLIANCE ATTACHMENT FIELDS
        // =============================================================================
        builder.Property(k => k.FrontImagePath)
            .IsRequired()
            .HasMaxLength(500); // Optimized path size allocation for secure cloud storage references

        builder.Property(k => k.BackImagePath)
            .IsRequired()
            .HasMaxLength(500);

        // ONSHORE PAKISTANI INVARIANT: Sanitized numeric characters stripped of hyphens down to exactly 13 digits
        builder.Property(k => k.SanitizedCnic)
            .IsRequired()
            .HasMaxLength(13)
            .IsFixedLength();

        // 🛠️ PROPERTY REALIGNMENT: Map the new identity tracking field added to support decoupled query handlers
        builder.Property(k => k.UserEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(k => k.Status)
            .IsRequired()
            .HasConversion<string>() // Persists KycStatus enum states (Pending, Approved, Unverified) as clear strings
            .HasMaxLength(30);

        builder.Property(k => k.RejectionReason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(k => k.AuditedByAgentEmail)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(k => k.AuditedAtUtc)
            .IsRequired(false);

        // =============================================================================
        // 🔗 RELATIONSHIP CONSTRAINT BOND (1-TO-ONE RELATIONAL LINK)
        // =============================================================================
        // Explicitly binds your verification documents straight back underneath the user profile tracking aggregate root
        builder.Property(k => k.UserProfileId)
            .IsRequired();

        builder.HasOne<UserProfile>()
            .WithOne() // Maps out a clean, decoupled 1-to-1 relationship boundary
            .HasForeignKey<KycDocument>(k => k.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================================================
        // 🚀 HIGH-SPEED PERFORMANCE SEARCH INDEXES
        // =============================================================================
        builder.HasIndex(k => k.UserProfileId)
            .IsUnique()
            .HasDatabaseName("IX_KycDocuments_UserProfileId");

        // 🛠️ SPEED INDEX LOOKUP: Accelerates direct filtering queries triggered via email handles
        builder.HasIndex(k => k.UserEmail)
            .HasDatabaseName("IX_KycDocuments_UserEmail");

        builder.HasIndex(k => k.SanitizedCnic)
            .HasDatabaseName("IX_KycDocuments_SanitizedCnic");

        // Highly performant sparse lookup index to streamline agent performance audits
        builder.HasIndex(k => k.AuditedByAgentEmail)
            .HasDatabaseName("IX_KycDocuments_AuditedByAgentEmail")
            .HasFilter("[AuditedByAgentEmail] IS NOT NULL");

        // Composite Index: Accelerates real-time compliance status short-circuit checks inside custom middleware gates
        builder.HasIndex(k => new { k.Status, k.UserEmail })
            .HasDatabaseName("IX_KycDocuments_Status_Billing_Telemetry");
    }
}