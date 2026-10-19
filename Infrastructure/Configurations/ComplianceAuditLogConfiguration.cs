using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ComplianceAuditLogConfiguration : IEntityTypeConfiguration<ComplianceAuditLog>
{
    public void Configure(EntityTypeBuilder<ComplianceAuditLog> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TargetEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ReviewerAgentEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.ActionPerformed)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(c => c.Comments)
            .HasMaxLength(1000);

        builder.Property(c => c.Timestamp)
            .IsRequired();

        // High-performance composite index for SECP audit queries
        builder.HasIndex(c => new { c.TargetEmail, c.Timestamp });
    }
}
