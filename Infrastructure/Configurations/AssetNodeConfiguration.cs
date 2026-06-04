using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.SqlServer;
namespace Infrastructure.Configurations;

public class AssetNodeConfiguration : IEntityTypeConfiguration<AssetNode>
{
    public void Configure(EntityTypeBuilder<AssetNode> builder)
    {
        builder.ToTable("Assets");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.UserEmail)
            .HasMaxLength(256)
            .IsRequired();

        // High-precision specification to support heavy domestic currency valuations (PKR) safely
        builder.Property(e => e.HoldingValuePKR)
            .HasPrecision(18, 2);

        // Core querying indices for quick calculation loops over individual profile views
        builder.HasIndex(e => e.UserEmail);
        builder.HasIndex(e => new { e.UserEmail, e.Category });
    }
}
