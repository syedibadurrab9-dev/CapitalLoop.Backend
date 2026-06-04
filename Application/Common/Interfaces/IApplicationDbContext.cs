using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<AssetNode> Assets { get; }
    DbSet<KycDocument> KycDocuments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
