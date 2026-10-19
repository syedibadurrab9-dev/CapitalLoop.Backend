using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<AssetNode> Assets { get; }
    DbSet<KycDocument> KycDocuments { get; }
    DbSet<ComplianceAuditLog> ComplianceAuditLogs { get; }
    DbSet<AdminOperator> AdminOperators { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
