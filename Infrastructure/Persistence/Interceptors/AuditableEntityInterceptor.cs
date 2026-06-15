using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    // Make this nullable to support design-time instantiation
    private readonly IIdentityService? _identityService;

    public AuditableEntityInterceptor(IIdentityService? identityService = null)
    {
        _identityService = identityService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditProperties(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        // 🛡️ DESIGN-TIME RESILIENCE: 
        // Resolve operator safely. Fallback to system daemon if identity service is null or fails.
        string activeOperator = "system-infrastructure-engine@capitalloop.internal";

        try
        {
            var email = _identityService?.GetCurrentUserEmail();
            if (!string.IsNullOrWhiteSpace(email))
            {
                activeOperator = email.Trim().ToLowerInvariant();
            }
        }
        catch
        {
            // Fail silently during design-time migration scans
        }

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Property(x => x.Id).CurrentValue = Guid.NewGuid();
                }
                entry.Entity.SetCreationAuditTelemetry(activeOperator);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkAsModified(activeOperator);
            }
        }
    }
}