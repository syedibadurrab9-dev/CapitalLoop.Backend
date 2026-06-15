using System;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// 🧱 FLUENT API DATABASE CONFIGURATION
/// Maps structural schema properties, decimal precisions, optional installment metrics, 
/// and physical precious metal weight fields for the AssetNode domain entity [CL-405].
/// </summary>
public class AssetNodeConfiguration : IEntityTypeConfiguration<AssetNode>
{
    public void Configure(EntityTypeBuilder<AssetNode> builder)
    {
        // =============================================================================
        // 🧱 PRIMARY CLUSTER KEY CONFIGURATIONS
        // =============================================================================
        builder.HasKey(a => a.Id);

        // =============================================================================
        // 🎛️ INHERITED AUDIT CONTEXT TELEMETRY FIELDS (ABAC MECHANICS)
        // =============================================================================
        builder.Property(a => a.CreatedByEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.LastModifiedByEmail)
            .IsRequired(false)
            .HasMaxLength(256);

        // =============================================================================
        // 📊 CORE MULTI-ASSET LEDGER ENTITY PROPERTIES
        // =============================================================================
        builder.Property(a => a.UserEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasConversion<string>() // Maps the AssetCategory enum (Crypto, Equities, PreciousMetal, RealEstate, Cash) as strings
            .HasMaxLength(50);

        // 🎯 RESTORED AND ENFORCED: ValuationPkr is a critical column saved directly in the database
        builder.Property(a => a.ValuationPkr)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // 🚀 CRITICAL INVARIANT IGNORE: TotalTolas is a read-only computed lambda property inside AssetNode.cs.
        // We explicitly ignore it here so the EF Core design-time migrator tool bypasses backing-field validation.
        builder.Ignore(a => a.TotalTolas);

        // Extra metadata field parameters required by sync commands
        builder.Property(a => a.Symbol)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(a => a.QuantityHoldings)
            .HasColumnType("decimal(18,6)") // Safe tracking ceiling for highly divisible assets like crypto fractions
            .IsRequired();

        builder.Property(a => a.IsAutoSynced)
            .IsRequired()
            .HasDefaultValue(false);

        // =============================================================================
        // 🏗️ OPTIONAL MODULE-SPECIFIC SCHEMA MAPPINGS (NULLABLE FIELDS)
        // =============================================================================

        // Real estate tracking metrics parameters
        builder.Property(a => a.InstallmentsPaid)
            .IsRequired(false);

        builder.Property(a => a.TotalInstallments)
            .IsRequired(false);

        // Traditional precious metals structural cost field (Silver / Gold purchase benchmarks)
        builder.Property(a => a.CostPerTola)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        // Automated API keys reference boundary mapping
        builder.Property(a => a.EncryptedApiKey)
            .HasMaxLength(1000) // Provides comfortable ceiling width for reversible v1_aes256 encrypted blobs
            .IsRequired(false);

        // =============================================================================
        // 🔗 RELATIONSHIP CONSTRAINT BOND (1-TO-MANY LINK)
        // =============================================================================
        // Synchronized cascade paths to match your main UserProfile principal tracking keys
        builder.HasOne<UserProfile>()
            .WithMany(p => p.AssetNodes)
            .HasForeignKey(a => a.UserEmail)
            .HasPrincipalKey(p => p.Email)
            .OnDelete(DeleteBehavior.Cascade);

        // =============================================================================
        // 🚀 HIGH-SPEED MULTI-TENANT QUERY OPTIMIZATION INDEXES
        // =============================================================================
        builder.HasIndex(a => a.UserEmail)
            .HasDatabaseName("IX_AssetNodes_UserEmail");

        // 📈 HIGH-FREQUENCY CALCULATION SCAN UPGRADE: 
        // Composite index explicitly tailored to accelerate your CQRS live forecast projections and dashboard aggregates.
        builder.HasIndex(a => new { a.UserEmail, a.Symbol, a.IsAutoSynced })
            .HasDatabaseName("IX_AssetNodes_MarketSync_Covering_Composite");
    }
}