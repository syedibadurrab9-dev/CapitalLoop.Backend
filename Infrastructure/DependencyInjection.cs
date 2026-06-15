using Application.Common.Interfaces;
using Application.Common.Interfaces.ComplianceAsyncJobs;
using Application.Common.Interfaces.ExternalIntegrations;
using Application.Common.Interfaces.MarketIntelligence;
using Application.Portfolios.Commands.QueueTaxAudit;
using Infrastructure.BackgroundWorkers;
using Infrastructure.Messaging.Producers;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Services.Caching;
using Infrastructure.Services.ExternalIntegrations;
using Infrastructure.Services.Identity;
using Infrastructure.Services.MarketIntelligence;
using Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Channels;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        bool isEfCoreToolingActive = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.FullName != null && a.FullName.StartsWith("Microsoft.EntityFrameworkCore.Design"));

        // =============================================================================
        // 1. DATA PERSISTENCE LAYER REGISTRATION
        // =============================================================================
        if (isEfCoreToolingActive)
        {
            // DESIGN-TIME: Bare-bones registration for EF tool migrations execution
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("Infrastructure")));
        }
        else
        {
            // RUNTIME: Register interceptor cleanly to avoid cross-dependent service initialization loops
            services.AddScoped<AuditableEntityInterceptor>();

            // 🎯 FIXED: Removed the (sp, options) callback containing sp.GetRequiredService().
            // This decouples the DbContext instantiation from immediate container resolution states.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Infrastructure"));
            });

            services.AddScoped<IIdentityService, IdentityService>();
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        // =============================================================================
        // 2. ECOSYSTEM UTILITIES & BACKGROUND PROCESSING WORKERS
        // =============================================================================
        if (!isEfCoreToolingActive)
        {
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, CacheService>();
            services.AddHttpClient();

            // High-Performance Thread Channels for FinTech Ingestion Queues
            services.AddSingleton(provider => System.Threading.Channels.Channel.CreateBounded<Guid>(1000));
            services.AddSingleton(provider => System.Threading.Channels.Channel.CreateBounded<TaxAuditQueueMessage>(1000));
            services.AddScoped<ITaxAuditChannelProducer, TaxAuditChannelProducer>();

            // Integrated Hosted Daemons
            services.AddHostedService<DatabaseMigrationWorker>();
            services.AddHostedService<TaxExecutionQueueProcessor>();

            // Feature Scrapers & Market Feeds
            services.AddScoped<IPsxScraperService, PsxScraperService>();
            services.AddScoped<IPreciousMetalSyncService, PreciousMetalSyncService>();
            services.AddScoped<IMarketDataService, MarketDataService>();
            services.AddScoped<ICryptoSyncService, CryptoSyncService>();
            services.AddScoped<IAkdBrokerageSyncService, AkdBrokerageSyncService>();
            services.AddScoped<IFbrScraperService, FbrScraperService>();
        }

        return services;
    }
}