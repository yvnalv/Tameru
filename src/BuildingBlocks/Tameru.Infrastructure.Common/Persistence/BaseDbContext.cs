using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.SharedKernel.Domain;
using Tameru.SharedKernel.Time;

namespace Tameru.Infrastructure.Common.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> for every module. Centralizes two cross-cutting concerns:
/// <list type="bullet">
///   <item>audit-field stamping (CreatedBy/At, UpdatedBy/At) on save;</item>
///   <item>soft delete — a physical delete of an <see cref="ISoftDeletable"/> is turned into a flag
///   update, and a global query filter hides deleted rows (CLAUDE.md rule #6).</item>
/// </list>
/// Also implements <see cref="IUnitOfWork"/> so use cases commit through a single abstraction.
/// </summary>
public abstract class BaseDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    protected BaseDbContext(DbContextOptions options, ICurrentUser currentUser, IClock clock)
        : base(options)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    /// <summary>
    /// Applies the soft-delete global query filter to every <see cref="ISoftDeletable"/> entity.
    /// Derived contexts must call this at the end of their own <see cref="OnModelCreating"/>.
    /// </summary>
    protected static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private void ApplyAuditAndSoftDelete()
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;

                case EntityState.Deleted:
                    // Never physically delete financial data — soft delete instead.
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    entry.Entity.DeletedBy = userId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }
    }
}
