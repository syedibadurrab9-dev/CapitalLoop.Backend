using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Standardizes high-frequency lookup keys, data scales, and composite index seeks 
/// for the MarketAssetPrice financial tracking entity [CL-407].
/// </summary>
public class MarketAssetPriceConfiguration : IEntityTypeConfiguration<MarketAssetPrice>
{
    public void Configure(EntityTypeBuilder<MarketAssetPrice> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(m => m.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(m => m.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(m => m.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE ASSET MARKERS & FINANCIAL DATA ATTRIBUTES
        // =============================================================================
        builder.Property(m => m.AssetTicker)
            .IsRequired()
            .HasMaxLength(30); // Provides clean space for localized tickers (e.g., "SILVER10TOLA", "ENGRO", "KSE100")

        builder.Property(m => m.AssetName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(m => m.AssetClass)
            .IsRequired()
            .HasMaxLength(50); // Categorization labels (e.g., "Equities", "PreciousMetals", "Crypto")

        // HIGH-PRECISION BANKING SCALE: Enforces strict 18,2 numeric storage to prevent rounding drift
        builder.Property(m => m.UnitPricePkr)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(m => m.LastSyncedUtc)
            .IsRequired();

        // =============================================================================
        // 🚀 HIGH-FREQUENCY SPOT SCAN OPTIMIZATION INDEXES
        // =============================================================================

        // Creates a unique optimized search index on the uppercase ticker key. This accelerates 
        // chart vectors, prevents collision anomalies, and quickens portfolio revaluations.
        builder.HasIndex(m => m.AssetTicker)
            .IsUnique()
            .HasDatabaseName("IX_MarketAssetPrices_AssetTicker_Unique");

        // Composite index optimized for background syncing batch routines sorting historical entries
        builder.HasIndex(m => new { m.AssetTicker, m.LastSyncedUtc })
            .HasDatabaseName("IX_MarketAssetPrices_Ticker_SyncHistory_Composite");
    }
}