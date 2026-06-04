using Domain.Entities;
using Domain.Enums; // Crucial for mapping your KycStatus enum targets
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AssetNode> Assets => Set<AssetNode>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    // ============================================================================
    // FIXED: Enums matched perfectly to Domain Spec (KycStatus.Approved)
    // ============================================================================
    public static async Task SeedDataAsync(ApplicationDbContext context)
    {
        // 1. Ensure the underlying database container exists
        await context.Database.EnsureCreatedAsync();

        const string targetUserEmail = "syedibad335@gmail.com";

        // 2. Step A: Seed User Profile using the Domain Factory 'Register' Method
        if (!await context.UserProfiles.AnyAsync(u => u.Email == targetUserEmail))
        {
            var seedUser = UserProfile.Register("Syed Ibad Ur Rab", targetUserEmail);

            // Match your custom criteria for premium tier testing eligibility
            seedUser.UpgradeToPremium();

            // FIX: Aligned perfectly to your Domain.Enums.KycStatus structure
            seedUser.UpdateKycStatus(KycStatus.Approved);

            await context.UserProfiles.AddAsync(seedUser);
            await context.SaveChangesAsync(); // Force immediate commit to satisfy reference integrity constraints
        }

        // 3. Step B: Seed Asset Nodes using the Domain Factory 'CreateManual' Method
        if (!await context.Assets.AnyAsync())
        {
            var seedAssets = new List<AssetNode>
            {
                // Crypto Portfolio Node
                AssetNode.CreateManual(targetUserEmail, "Bitcoin Spot Holding", AssetCategory.Crypto, 3500000m),
                
                // Silver Tracker Node
                AssetNode.CreateManual(targetUserEmail, "10 Tolas Physical Silver", AssetCategory.Precious_Metal, 152500m),
                
                // Real Estate Installment Telemetry Node
                AssetNode.CreateManual(targetUserEmail, "Lahore Residential Plot Plan", AssetCategory.Real_Estate, 1200000m, paid: 15, total: 120)
            };

            await context.Assets.AddRangeAsync(seedAssets);
        }

        // 4. Step C: Seed Verification KYC Documents using the Domain Factory 'RecordSubmission' Method
        if (!await context.KycDocuments.AnyAsync())
        {
            var seedKyc = KycDocument.RecordSubmission(
                email: targetUserEmail,
                type: "Smart_CNIC",
                docNum: "35201-XXXXXXX-X", // Mock national identification schema target
                path: "https://s3.amazonaws.com/capitalloop-kyc-bucket/prod/identities/ibad_cnic.jpg"
            );

            await context.KycDocuments.AddAsync(seedKyc);
        }

        // Finalize state modifications cleanly on your host storage ledger
        await context.SaveChangesAsync();
    }
}