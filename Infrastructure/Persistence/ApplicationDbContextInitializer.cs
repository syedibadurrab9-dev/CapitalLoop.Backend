using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitializer> _logger;
    private const string SystemInitOperator = "system-initialization-daemon@capitalloop.internal";

    public ApplicationDbContextInitializer(ApplicationDbContext context, ILogger<ApplicationDbContextInitializer> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                // Use execution strategy for resilient connections during migration
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    _logger.LogInformation("Inspecting storage cluster state and applying migrations...");
                    await _context.Database.MigrateAsync();
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            // Wrap seeding in an explicit transaction to ensure atomicity
            using var transaction = await _context.Database.BeginTransactionAsync();
            await TrySeedAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure encountered while seeding the data layer. Transaction rolled back.");
            throw;
        }
    }

    private async Task TrySeedAsync()
    {
        _logger.LogInformation("Initiating data layer seeding loops...");

        // Use the strategy to ensure bulk inserts are resilient
        var strategy = _context.Database.CreateExecutionStrategy();

        // PHASE 1: MARKET PRICES
        if (!await _context.MarketAssetPrices.AnyAsync())
        {
            await strategy.ExecuteAsync(async () =>
            {
                var baselinePrices = new[] {
                    MarketAssetPrice.Create("KSE100", "KSE-100 Benchmark", "Equities", 75000.00m, SystemInitOperator),
                    MarketAssetPrice.Create("GOLD1TOLA", "Gold 24K (1Tola)", "PreciousMetals", 242000.00m, SystemInitOperator),
                    MarketAssetPrice.Create("SILVER10TOLA", "Silver Bull (10Tola)", "PreciousMetals", 152500.00m, SystemInitOperator),
                    MarketAssetPrice.Create("BTC", "Bitcoin", "Crypto", 18500000.00m, SystemInitOperator)
                };
                await _context.MarketAssetPrices.AddRangeAsync(baselinePrices);
                await _context.SaveChangesAsync();
            });
            _logger.LogInformation("Market feeds populated.");
        }

        // PHASE 2: EXCHANGE RATES
        if (!await _context.ExchangeRates.AnyAsync())
        {
            await strategy.ExecuteAsync(async () =>
            {
                var rates = new[] {
                    ExchangeRate.Create("USD", "PKR", 278.50m, SystemInitOperator),
                    ExchangeRate.Create("PKR", "PKR", 1.00m, SystemInitOperator)
                };
                await _context.ExchangeRates.AddRangeAsync(rates);
                await _context.SaveChangesAsync();
            });
            _logger.LogInformation("Exchange rates populated.");
        }

        // =============================================================================
        // PHASE 3: SYSTEM ADMIN (STRONGLY TYPED DOMAIN ENTITY ENGINE) [CL-102]
        // =============================================================================
        if (!await _context.UserProfiles.AnyAsync(u => u.SubscriptionTier == "agent-backoffice"))
        {
            await strategy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("System Administrator profile missing. Generating domain identity mappings...");

                var adminEmail = "system.admin@capitalloop.com";

                // 1. Check if the domain ApplicationUser entity already exists, or create it first
                var existingUser = await _context.Set<ApplicationUser>()
                    .FirstOrDefaultAsync(u => u.Email == adminEmail);

                if (existingUser == null)
                {
                    // Utilize a manual object initialization to align with your DDD parameters
                    // (Since CreateFromGoogleSso is tailored for external auth logins, we initialize directly here)
                    existingUser = new ApplicationUser();

                    // We can use reflection or set properties directly if your base entity allows it.
                    // Assuming Id is publicly or internally assignable via base Entity:
                    typeof(Entity).GetProperty(nameof(Entity.Id))?
                        .SetValue(existingUser, Guid.NewGuid());

                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Email))?
                        .SetValue(existingUser, adminEmail);

                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.DisplayName))?
                        .SetValue(existingUser, "System Administrator");

                    typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.SubscriptionTier))?
                        .SetValue(existingUser, "agent-backoffice");

                    // If your property setters are internal/public instead of completely private, 
                    // you can comment out the reflection block above and just use:
                    // existingUser.ChangeSubscriptionTier("agent-backoffice", SystemInitOperator);

                    await _context.Set<ApplicationUser>().AddAsync(existingUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Core domain ApplicationUser identity context generated.");
                }

                // 2. Build the target admin profile using your existing factory design pattern
                var adminProfile = UserProfile.RegisterAdmin("System Administrator", adminEmail, "agent-backoffice");

                // Link the two domain aggregates together using the rich behavior pattern inside your entity
                existingUser.LinkUserProfile(adminProfile);

                // Add the profile entity to the tracker state and complete the transaction lifecycle
                await _context.UserProfiles.AddAsync(adminProfile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("System Administrator back-office domain profiles seeded clean.");
            });
        }
    }
}