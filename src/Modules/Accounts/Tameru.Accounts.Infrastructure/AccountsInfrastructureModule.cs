using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tameru.Accounts.Application;
using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Infrastructure.Persistence;
using Tameru.Accounts.Infrastructure.Seeding;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Accounts.Infrastructure;

/// <summary>DI registration for the Accounts module.</summary>
public static class AccountsInfrastructureModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<AccountsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", AccountsDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IAccountsUnitOfWork>(sp => sp.GetRequiredService<AccountsDbContext>());
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAccountGroupRepository, AccountGroupRepository>();

        // Provided cross-module contract.
        services.AddScoped<IAccountDirectory, AccountDirectory>();

        // Consumed cross-module contract: default no-op until the Ledger module replaces it (M3).
        services.TryAddScoped<ILedgerAccountQuery, NoOpLedgerAccountQuery>();

        services.AddScoped<AccountService>();
        services.AddScoped<AccountsSeeder>();

        return services;
    }
}
