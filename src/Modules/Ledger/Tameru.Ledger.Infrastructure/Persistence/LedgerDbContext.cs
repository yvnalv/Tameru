using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.Infrastructure.Common.Persistence;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Domain;
using Tameru.Ledger.Infrastructure.Persistence.Configurations;
using Tameru.SharedKernel.Time;

namespace Tameru.Ledger.Infrastructure.Persistence;

/// <summary>EF Core context for the Ledger module. Owns the <c>ledger</c> schema.</summary>
public sealed class LedgerDbContext : BaseDbContext, ILedgerUnitOfWork
{
    public const string Schema = "ledger";

    public LedgerDbContext(DbContextOptions<LedgerDbContext> options, ICurrentUser currentUser, IClock clock)
        : base(options, currentUser, clock)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());

        ApplySoftDeleteFilter(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
