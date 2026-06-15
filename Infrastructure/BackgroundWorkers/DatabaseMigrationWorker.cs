using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundWorkers;

public class DatabaseMigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationWorker> _logger;

    public DatabaseMigrationWorker(IServiceProvider serviceProvider, ILogger<DatabaseMigrationWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[STARTUP] Database migration lifecycle monitoring initiated.");

        // 🎯 Yield control back to the .NET Host engine immediately!
        // This pushes the execution of this method to the background thread pool queue,
        // allowing Kestrel to finish its boot configurations cleanly and freeing up the root DI container.
        await Task.Yield();

        try
        {
            // Give Kestrel a moment to cleanly establish its network sockets first
            await Task.Delay(2000, stoppingToken);

            _logger.LogInformation("[STARTUP] Initializing isolated database automation cycle...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var initLogger = loggerFactory.CreateLogger<ApplicationDbContextInitializer>();

                _logger.LogInformation("[STARTUP] Building clean context options...");

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Infrastructure")
                );

                // 🎯 THE FIX: Instantiation signature matching the new decoupled ApplicationDbContext constructor.
                // We no longer explicitly resolve the interceptor here, completely preventing 
                // early boot DI container deadlocks.
                using (var context = new ApplicationDbContext(optionsBuilder.Options, scope.ServiceProvider))
                {
                    _logger.LogInformation("[STARTUP] Manually instantiating Initializer...");
                    var initializer = new ApplicationDbContextInitializer(context, initLogger);

                    _logger.LogInformation("[STARTUP] Invoking InitializeAsync (EF Core Migrate)...");
                    await initializer.InitializeAsync();
                    _logger.LogInformation("[STARTUP] InitializeAsync complete.");

                    _logger.LogInformation("[STARTUP] Invoking SeedAsync (Data Layer Seeding)...");
                    await initializer.SeedAsync();
                    _logger.LogInformation("[STARTUP] SeedAsync complete.");
                } // 🔓 Database connection explicitly closed here
            } // 🔓 DI container allocation scope completely released here

            _logger.LogInformation("[STARTUP] CapitalLoop database hydration completed cleanly. Ready for API entries.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[STARTUP] CRITICAL FATAL: Background storage hydration engine collapsed.");
        }
    }
}