namespace DigitalWallet.Infrastructure.Data.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;
using DigitalWallet.Domain.Common;
using Microsoft.EntityFrameworkCore;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null) return;
        var now = DateTime.Now;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Deleted) continue;
        
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }                
        }
    }
}