using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tameru.Application.Abstractions;
using Tameru.SharedKernel.Time;

namespace Tameru.Budgeting.Infrastructure.Persistence;

/// <summary>Lets EF Core tooling create the context without booting the API. Not used at runtime.</summary>
public sealed class DesignTimeBudgetingDbContextFactory : IDesignTimeDbContextFactory<BudgetingDbContext>
{
    public BudgetingDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=tameru;Username=tameru;Password=tameru_dev";

        var options = new DbContextOptionsBuilder<BudgetingDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BudgetingDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new BudgetingDbContext(options, new DesignTimeCurrentUser(), new SystemClock());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;

        public bool IsAuthenticated => false;
    }
}
