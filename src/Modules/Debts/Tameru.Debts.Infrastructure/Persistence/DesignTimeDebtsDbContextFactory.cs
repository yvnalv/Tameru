using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tameru.Application.Abstractions;
using Tameru.SharedKernel.Time;

namespace Tameru.Debts.Infrastructure.Persistence;

public sealed class DesignTimeDebtsDbContextFactory : IDesignTimeDbContextFactory<DebtsDbContext>
{
    public DebtsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=tameru;Username=tameru;Password=tameru_dev";

        var options = new DbContextOptionsBuilder<DebtsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", DebtsDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DebtsDbContext(options, new DesignTimeCurrentUser(), new SystemClock());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }
}
