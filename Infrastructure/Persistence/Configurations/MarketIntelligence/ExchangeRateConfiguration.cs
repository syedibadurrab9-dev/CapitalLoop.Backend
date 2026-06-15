using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties and exact decimal precisions for the ExchangeRate entity [CL-201].
/// </summary>
public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(e => e.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(e => e.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE EXCHANGE RATE PROPERTIES & PRECISION
        // =============================================================================
        builder.Property(e => e.FromCurrency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.ToCurrency)
            .IsRequired()
            .HasMaxLength(10);

        // 🎯 RESOLVES WARNING 30000 PERMANENTLY: Enforces high-precision forex scale mapping
        // 'decimal(18,6)' ensures interbank fractions (e.g., USD to PKR 278.532500) preserve their complete precision layout.
        builder.Property(e => e.Rate)
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(e => e.LastUpdatedUtc)
            .IsRequired();

        // =============================================================================
        // 🚀 HIGH-SPEED PERFORMANCE SEARCH INDEXES
        // =============================================================================
        // Unique Composite Index: Prevents data duplication anomalies for any given currency pair mapping
        builder.HasIndex(e => new { e.FromCurrency, e.ToCurrency })
            .IsUnique()
            .HasDatabaseName("IX_ExchangeRates_CurrencyPair_Unique_Composite");
    }
}