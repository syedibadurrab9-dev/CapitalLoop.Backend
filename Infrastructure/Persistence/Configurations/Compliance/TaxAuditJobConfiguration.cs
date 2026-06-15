using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties, baseline tracking strings, and background worker queue indices
/// for the updated TaxAuditJob domain entity [CL-409].
/// </summary>
public class TaxAuditJobConfiguration : IEntityTypeConfiguration<TaxAuditJob>
{
    public void Configure(EntityTypeBuilder<TaxAuditJob> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(t => t.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(t => t.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE TASK JOB ATTRIBUTES & SYSTEM CONTROL FLAGS
        // =============================================================================
        builder.Property(t => t.UserEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.InitiatedBySystem)
            .IsRequired()
            .HasDefaultValue(false);

        // ENGINE STATE SAFEGUARD: Locks down column lengths for status trackers 
        // to prevent open-ended NVARCHAR(MAX) page breaks inside index structures.
        builder.Property(t => t.ExecutionStatus)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("Queued"); // Synchronizes database defaults with your domain constructor

        builder.Property(t => t.ProcessedAtUtc)
            .IsRequired(false);

        // =============================================================================
        // 🚀 ASYNCHRONOUS DAEMON WORKER OPTIMIZATION INDEXES
        // =============================================================================

        // Creates a highly performant composite index to let background scrapers and FBR filing alignment components 
        // instantly fetch the next item in the processing queue chronologically without executing full table scans.
        builder.HasIndex(t => new { t.ExecutionStatus, t.CreatedAt })
            .HasDatabaseName("IX_TaxAuditJobs_QueueProcessor_Composite");

        // Multi-tenant indexing to easily query or filter calculation histories for an individual profile
        builder.HasIndex(t => t.UserEmail)
            .HasDatabaseName("IX_TaxAuditJobs_UserEmail");
    }
}