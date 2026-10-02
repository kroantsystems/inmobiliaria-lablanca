using LaBlanca.Application.Abstractions.Persistence;
using LaBlanca.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LaBlanca.Infrastructure.Persistence;

/// <summary>Preenche tenant e datas de auditoria e impede gravar ou mover dados para outro tenant.</summary>
internal sealed class TenantAuditInterceptor(ITenantContext tenantContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var tenantId = tenantContext.TenantId;
        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<TenantEntity>())
        {
            var tenant = entry.Property(e => e.TenantId);
            if (entry.State == EntityState.Added)
            {
                if (tenantId != Guid.Empty)
                {
                    tenant.CurrentValue = tenantId;
                }
                else if (tenant.CurrentValue == Guid.Empty)
                {
                    throw new InvalidOperationException($"Cannot save {entry.Metadata.ClrType.Name} without a tenant.");
                }
            }
            else if (entry.State == EntityState.Modified && tenant.IsModified)
            {
                throw new InvalidOperationException($"Changing the tenant of {entry.Metadata.ClrType.Name} is not allowed.");
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }
        }
    }
}
