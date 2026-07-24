using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Domain;
using Tameru.Infrastructure.Common.Persistence;
using Tameru.SharedKernel.Time;

namespace Tameru.Budgeting.Infrastructure.Persistence;

/// <summary>EF Core context for the Budgeting module. Owns the <c>budgeting</c> schema.</summary>
public sealed class BudgetingDbContext : BaseDbContext, IBudgetingUnitOfWork
{
    public const string Schema = "budgeting";

    public BudgetingDbContext(DbContextOptions<BudgetingDbContext> options, ICurrentUser currentUser, IClock clock)
        : base(options, currentUser, clock)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<BudgetPeriod> BudgetPeriods => Set<BudgetPeriod>();

    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();

    public DbSet<MasterPlanSection> MasterPlanSections => Set<MasterPlanSection>();

    public DbSet<MasterPlanItem> MasterPlanItems => Set<MasterPlanItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetingDbContext).Assembly);

        ApplySoftDeleteFilter(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
