using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Application.Common.Interfaces;

/// <summary>
/// 🏛️ CENTRAL STORAGE EXTENSION ABSTRACT CONTRACT BOUNDARY [CL-205]
/// Inverts persistence dependencies by exposing operational database tables, 
/// relational tracking datasets, change tracking state metrics, and transaction contexts 
/// cleanly to downstream application use-case command and query layers.
/// </summary>
public interface IApplicationDbContext
{
    // =============================================================================
    // 🔐 IDENTITY & SECURITY PERSISTENCE ENTITY TABLES
    // =============================================================================

    /// <summary>
    /// Gets the secure authentication registry mapping system credentials, security claims, and platform roles [CL-503].
    /// Anchors your Google SSO account validation targets directly to structural database keys.
    /// </summary>
    DbSet<ApplicationUser> Users { get; }

    /// <summary>
    /// Gets the retail business metadata entity partition tracking consumer configurations independent of security profiles [CL-101].
    /// </summary>
    DbSet<UserProfile> UserProfiles { get; }

    // =============================================================================
    // 💼 PORTFOLIO ASSET LEDGER PERSISTENCE ENTITY TABLES
    // =============================================================================

    /// <summary>
    /// Gets the master asset node repository partition mapping back to multi-wallet operational ledger positions [CL-201 / CL-203].
    /// Pluralized cleanly to match your CQRS asset list telemetry lookups.
    /// </summary>
    DbSet<AssetNode> AssetNodes { get; }

    /// <summary>
    /// Gets the fiat translation rate registry capturing currency desk valuations (e.g., Al Habib Foreign Exchange Sync).
    /// </summary>
    DbSet<ExchangeRate> ExchangeRates { get; }

    /// <summary>
    /// Gets the high-frequency spot valuation records capturing equities, crypto units, and commodity spot-rates (e.g., PSX, Binance APIs).
    /// </summary>
    DbSet<MarketAssetPrice> MarketAssetPrices { get; }

    /// <summary>
    /// Gets the cryptographically signed, read-only secure sharing links allowing external peer reviews [CL-209].
    /// Fully qualified matching path layout definitions to resolve Roslyn cross-assembly interface mapping errors.
    /// </summary>
    DbSet<PortfolioShareLink> PortfolioShareLinks { get; }

    // =============================================================================
    // ⚖️ REGULATORY COMPLIANCE & COMMUNICATION PERSISTENCE ENTITY TABLES
    // =============================================================================

    /// <summary>
    /// Gets the physical onboarding verification record matrix containing extracted CNIC hashes and OCR validation tracks [CL-104].
    /// </summary>
    DbSet<KycDocument> KycDocuments { get; }

    /// <summary>
    /// Gets the persistent real-time conversational message thread logs captured over the full-duplex SignalR support gateway [CL-401].
    /// </summary>
    DbSet<ChatMessage> ChatMessages { get; }

    /// <summary>
    /// Gets the advisor workspace consulting ledgers tracking early-funnel calendar synchronization slots [CL-502].
    /// </summary>
    DbSet<AdvisorConsultation> AdvisorConsultations { get; }

    /// <summary>
    /// Gets the asynchronous background tax optimization audit jobs lifecycle tracker ledger [CL-104].
    /// </summary>
    DbSet<TaxAuditJob> TaxAuditJobs { get; }

    // =============================================================================
    // 🎛️ UNIT-OF-WORK STATE MANAGEMENT & TRANSACTION MATRIX CONTRACTS
    // =============================================================================

    /// <summary>
    /// Exposes the underlying Entity Framework database engine change-tracking ecosystem natively.
    /// Used to explicitly modify state properties or enforce high-speed .AsNoTracking() query streams.
    /// </summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>
    /// Exposes access to the underlying facade wrapper utilities (e.g., Database.BeginTransactionAsync).
    /// Crucial for guaranteeing atomic isolation loops across multiple database writes inside custom CQRS handlers.
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// Persists all tracked relational transactional alterations down to physical database layers safely.
    /// Automatically applies user email security telemetry to mutated aggregate fields at save runtime.
    /// </summary>
    /// <param name="cancellationToken">Propagates notification that operational threads should be canceled.</param>
    /// <returns>The total count of structural state modification records committed cleanly to SQL Server.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronous persistence alternative used strictly within specialized utility components or infrastructure check routines.
    /// </summary>
    /// <returns>The total count of structural state modification records committed cleanly to SQL Server.</returns>
    int SaveChanges();
}