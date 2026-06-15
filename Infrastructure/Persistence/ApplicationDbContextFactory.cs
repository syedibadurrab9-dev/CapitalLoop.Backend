using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Locate appsettings.json
        // Using Path.GetFullPath ensures we resolve the relative path correctly from the project root
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "WebApi"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 2. Extract connection string with a fallback or clear error
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new System.InvalidOperationException("Could not find 'DefaultConnection' in appsettings.json. Ensure the connection string is defined.");
        }

        // 3. Configure SQL Server
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            connectionString,
            b => b.MigrationsAssembly("Infrastructure"));

        // 4. Return the Context with design-time stub arguments
        // 🎯 THE FIX: Signature aligned to the updated two-argument constructor (options, serviceProvider).
        // Passing null! for the service provider is safe during design-time operations since EF CLI 
        // migrations generation cycles only mirror structural model metadata definitions.
        return new ApplicationDbContext(optionsBuilder.Options, null!);
    }
}