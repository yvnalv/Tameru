using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.Debts.Application.Abstractions;
using Tameru.Debts.Domain;
using Tameru.Debts.Infrastructure.Persistence.Configurations;
using Tameru.Infrastructure.Common.Persistence;
using Tameru.SharedKernel.Time;

namespace Tameru.Debts.Infrastructure.Persistence;

/// <summary>EF Core context for the Debts module. Owns the <c>debts</c> schema.</summary>
public sealed class DebtsDbContext : BaseDbContext, IDebtsUnitOfWork
{
    public const string Schema = "debts";

    public DebtsDbContext(DbContextOptions<DebtsDbContext> options, ICurrentUser currentUser, IClock clock)
        : base(options, currentUser, clock)
    {
    }

    public DbSet<Liability> Liabilities => Set<Liability>();
    public DbSet<LiabilityPayment> LiabilityPayments => Set<LiabilityPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new LiabilityConfiguration());
        modelBuilder.ApplyConfiguration(new LiabilityPaymentConfiguration());

        ApplySoftDeleteFilter(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
