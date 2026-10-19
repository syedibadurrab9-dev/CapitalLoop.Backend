using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.ToTable("KycDocuments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.DocumentType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DocumentNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.StorageUrlPath)
            .HasMaxLength(1000)
            .IsRequired();

        // Query performance indices for compliance audits and duplicate detection (O(1) index scans)
        builder.HasIndex(e => e.UserEmail);
        builder.HasIndex(e => e.DocumentNumber);
        builder.HasIndex(e => new { e.UserEmail, e.DocumentType });
    }
}
