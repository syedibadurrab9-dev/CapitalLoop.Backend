using Application.Common.Interfaces;
using Infrastructure.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Compliance.Queries.GetComplianceQueue;

public record UserComplianceDto(
    string Email,
    string Name,
    string KycStatus,
    string? DocumentUrl,
    DateTime SubmittedAt,
    string? DocumentType,
    string? DocumentNumber
);

public record GetComplianceQueueQuery : IQuery<List<UserComplianceDto>>;

public class GetComplianceQueueQueryHandler : IQueryHandler<GetComplianceQueueQuery, List<UserComplianceDto>>
{
    private readonly IApplicationDbContext _context;

    public GetComplianceQueueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserComplianceDto>> HandleAsync(GetComplianceQueueQuery query, CancellationToken cancellationToken = default)
    {
        // 1. Fetch user profiles using AsNoTracking for read-optimized telemetry
        var users = await _context.UserProfiles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // 2. Fetch all KYC documents in a single batch query (Zero N+1 optimization)
        var kycDocs = await _context.KycDocuments
            .AsNoTracking()
            .OrderByDescending(d => d.SubmittedUtc)
            .ToListAsync(cancellationToken);

        // 3. Map in memory with O(N+M) dictionary lookup
        var latestDocsByUser = kycDocs
            .GroupBy(d => d.UserEmail)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new List<UserComplianceDto>(users.Count);

        foreach (var user in users)
        {
            latestDocsByUser.TryGetValue(user.Email, out var doc);

            result.Add(new UserComplianceDto(
                Email: user.Email,
                Name: user.Name,
                KycStatus: user.KycStatus.ToString().ToUpperInvariant(),
                DocumentUrl: doc?.StorageUrlPath,
                SubmittedAt: doc?.SubmittedUtc ?? user.JoinedUtc,
                DocumentType: doc?.DocumentType,
                DocumentNumber: doc?.DocumentNumber
            ));
        }

        return result;
    }
}
