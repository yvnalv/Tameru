using Microsoft.EntityFrameworkCore;
using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Domain;
using Tameru.Accounts.Infrastructure.Persistence.Configurations;
using Tameru.Application.Abstractions;
using Tameru.Infrastructure.Common.Persistence;
using Tameru.SharedKernel.Time;

namespace Tameru.Accounts.Infrastructure.Persistence;

/// <summary>EF Core context for the Accounts module. Owns the <c>accounts</c> schema.</summary>
public sealed class AccountsDbContext : BaseDbContext, IAccountsUnitOfWork
{
    public const string Schema = "accounts";

    public AccountsDbContext(DbContextOptions<AccountsDbContext> options, ICurrentUser currentUser, IClock clock)
        : base(options, currentUser, clock)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<AccountGroup> AccountGroups => Set<AccountGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new AccountGroupConfiguration());

        ApplySoftDeleteFilter(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
