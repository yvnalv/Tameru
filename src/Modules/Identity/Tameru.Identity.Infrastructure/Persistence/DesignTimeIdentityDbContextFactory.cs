using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tameru.Application.Abstractions;
using Tameru.SharedKernel.Time;

namespace Tameru.Identity.Infrastructure.Persistence;

/// <summary>
/// Lets EF Core tooling (<c>dotnet ef migrations</c>) create the context without booting the API.
/// Uses <c>ConnectionStrings__Postgres</c> if set, else a local default. Not used at runtime.
/// </summary>
public sealed class DesignTimeIdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=tameru;Username=tameru;Password=tameru_dev";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new IdentityDbContext(options, new DesignTimeCurrentUser(), new SystemClock());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid UserId => Guid.Empty;

        public bool IsAuthenticated => false;
    }
}
