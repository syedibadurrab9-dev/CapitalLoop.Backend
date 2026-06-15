using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IServiceProvider _serviceProvider;

    // 🎯 DEADLOCK ABSOLUTELY ELIMINATED: Remove the constructor parameters that force 
    // eager container evaluations. The IoC engine can now instantiate this class instantly.
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    // =============================================================================
    // 🔐 IDENTITY & SECURITY PERSISTENCE ENTITY TABLES
    // =============================================================================
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // =============================================================================
    // 💼 PORTFOLIO ASSET LEDGER PERSISTENCE ENTITY TABLES
    // =============================================================================
    public DbSet<AssetNode> AssetNodes => Set<AssetNode>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<MarketAssetPrice> MarketAssetPrices => Set<MarketAssetPrice>();
    public DbSet<PortfolioShareLink> PortfolioShareLinks => Set<PortfolioShareLink>();

    // =============================================================================
    // ⚖️ REGULATORY COMPLIANCE & COMMUNICATION PERSISTENCE ENTITY TABLES
    // =============================================================================
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AdvisorConsultation> AdvisorConsultations => Set<AdvisorConsultation>();
    public DbSet<TaxAuditJob> TaxAuditJobs => Set<TaxAuditJob>();

    // =============================================================================
    // 🎛️ UNIT-OF-WORK STATE MANAGEMENT & TRANSACTION MATRIX CONTRACTS
    // =============================================================================
    public new ChangeTracker ChangeTracker => base.ChangeTracker;

    DatabaseFacade IApplicationDbContext.Database => base.Database;

    // =============================================================================
    // 🎛️ CONFIGURATION & PIPELINE INTERCEPTOR ATTACHMENTS
    // =============================================================================
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        // 🚀 SAFE LAZY RESOLUTION: Pull the interceptor dynamically out of the active scope 
        // ONLY if it is registered, completely isolating the initialization phase from 
        // deep cyclic validation tree sweeps.
        var interceptor = _serviceProvider.GetService<AuditableEntityInterceptor>();
        if (interceptor != null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    // =============================================================================
    // 💾 TRANSACTION SAVE BOUNDARY CONTROLLERS
    // =============================================================================
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTelemetryTracks();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditTelemetryTracks();
        return base.SaveChanges();
    }

    private void ApplyAuditTelemetryTracks()
    {
        string activeOperator = "system-infrastructure-engine@capitalloop.internal";

        try
        {
            // 🎯 LAZY LOOKUP: Fetch the identity service safely directly out of the service provider scope
            // only at the exact millisecond we are committing data records.
            var identityService = _serviceProvider.GetService<IIdentityService>();

            var email = identityService?.GetCurrentUserEmail();
            if (!string.IsNullOrWhiteSpace(email))
            {
                activeOperator = email.Trim().ToLowerInvariant();
            }
        }
        catch
        {
            // Fail silently to ensure persistence logic doesn't break migration scans
        }

        foreach (var trackingEntry in ChangeTracker.Entries<Entity>())
        {
            if (trackingEntry.State == EntityState.Added)
            {
                trackingEntry.Entity.SetCreationAuditTelemetry(activeOperator);
            }
            else if (trackingEntry.State == EntityState.Modified)
            {
                trackingEntry.Entity.MarkAsModified(activeOperator);
            }
        }
    }
}