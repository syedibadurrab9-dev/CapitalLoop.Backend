using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties, explicit string bounds, and dual-tenant search indices 
/// for the updated AdvisorConsultation workspace scheduling domain entity [CL-411].
/// </summary>
public class AdvisorConsultationConfiguration : IEntityTypeConfiguration<AdvisorConsultation>
{
    public void Configure(EntityTypeBuilder<AdvisorConsultation> builder)
    {
        // =============================================================================
        // 🧱 TABLE DESIGNATION & PRIMARY CLUSTER KEY TRACKING BOUNDS
        // =============================================================================
        builder.ToTable("AdvisorConsultations");
        builder.HasKey(c => c.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(c => c.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE BUSINESS DOMAIN ELEMENT ATTRIBUTES
        // =============================================================================
        builder.Property(c => c.ProspectName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.ProspectEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.TargetedAssetClass)
            .IsRequired()
            .HasMaxLength(50); // Bounds classifications (e.g., "Equities", "PreciousMetals", "Crypto")

        builder.Property(c => c.RequestedCallSlotUtc)
            .IsRequired();

        builder.Property(c => c.IsAssignedToAgent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.AssignedAgentEmail)
            .HasMaxLength(150)
            .IsRequired(false); // Nullable until an enterprise agent executes a claim handshake

        // =============================================================================
        // 🚀 ARCHITECTURAL SEARCH & CLASH PROTECTION INDICES
        // =============================================================================

        // Speeds up user dashboard lookup queries when fetching upcoming appointments by email
        builder.HasIndex(c => c.ProspectEmail)
            .HasDatabaseName("IX_AdvisorConsultations_ProspectEmail");

        // Optimizes back-office administrative desk queues rendering assigned advisor workloads via sparse index
        builder.HasIndex(c => c.AssignedAgentEmail)
            .HasDatabaseName("IX_AdvisorConsultations_AssignedAgentEmail")
            .HasFilter("[AssignedAgentEmail] IS NOT NULL");

        // 🚀 CRITICAL SCHEDULING CLASH DEFENSE:
        // Highly optimized composite index sorting calendar records chronologically. 
        // Allows your reservation pipelines to instantly evaluate an advisor's daily availability matrix,
        // permanently safeguarding against double-booking data race claims down in the database engine.
        builder.HasIndex(c => new { c.AssignedAgentEmail, c.RequestedCallSlotUtc })
            .HasDatabaseName("IX_AdvisorConsultations_Schedule_Clash_Composite")
            .HasFilter("[AssignedAgentEmail] IS NOT NULL");
    }
}