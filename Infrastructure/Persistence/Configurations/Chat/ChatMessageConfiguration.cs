using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties, explicit string bounds, and high-performance indexing channels
/// for the updated ChatMessage SignalR domain entity [CL-410].
/// </summary>
public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
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
        // 📊 CORE COMMUNICATION CONTENT MATRIX CONSTRAINT FIELDS
        // =============================================================================
        builder.Property(c => c.SenderEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.ReceiverEmail)
            .IsRequired()
            .HasMaxLength(150);

        // CORE COMMUNICATION CONTENT BOUNDARY: Optimized space allocations for chat texts
        builder.Property(c => c.MessageText)
            .IsRequired()
            .HasMaxLength(4000); // Caps chat items to avoid open NVARCHAR(MAX) page fragmentation

        // EMBEDDED GRAPHICS HEADROOM: Allocates large string tracking spaces for transient base64 components
        builder.Property(c => c.ChartTokenPayload)
            .IsRequired(false)
            .HasColumnType("nvarchar(max)"); // Tailored to bind embedded UI charts directly into database logs

        builder.Property(c => c.IsAcknowledgedByOperator)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        // =============================================================================
        // 🚀 HIGH-SPEED PERFORMANCE SEARCH & REAL-TIME SEEK INDEXES
        // =============================================================================
        builder.HasIndex(c => c.SenderEmail)
            .HasDatabaseName("IX_ChatMessages_SenderEmail");

        builder.HasIndex(c => c.ReceiverEmail)
            .HasDatabaseName("IX_ChatMessages_ReceiverEmail");

        // 🧱 REALIGNED COMPOSITE THREAD INDEX: Chronologically pairs message flows using 'CreatedAt'
        // Satisfies and speeds up your type-safe GetChatHistoryQuery execution pathways perfectly.
        builder.HasIndex(c => new { c.SenderEmail, c.ReceiverEmail, c.CreatedAt })
            .HasDatabaseName("IX_ChatMessages_Thread_Chronological_Composite");

        // Accelerates background scalar checks that compute real-time unread application badges via sparse index
        builder.HasIndex(c => new { c.ReceiverEmail, c.IsRead })
            .HasDatabaseName("IX_ChatMessages_Unread_Badge_Sparse_Composite")
            .HasFilter("[IsRead] = 0");

        // Speeds up back-office administration dashboards handling unaddressed support tickets via sparse index
        builder.HasIndex(c => c.IsAcknowledgedByOperator)
            .HasDatabaseName("IX_ChatMessages_Unacknowledged_Queue_Sparse_Composite")
            .HasFilter("[IsAcknowledgedByOperator] = 0");
    }
}