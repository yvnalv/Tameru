using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tameru.Ledger.Application;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Ledger.Infrastructure;

/// <summary>DI registration for the Ledger module.</summary>
public static class LedgerInfrastructureModule
{
    /// <summary>
    /// Registers the Ledger module. Call this <b>after</b> <c>AddAccountsModule</c> so the real
    /// <see cref="ILedgerAccountQuery"/> here replaces the Accounts module's no-op default.
    /// </summary>
    public static IServiceCollection AddLedgerModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", LedgerDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<ILedgerUnitOfWork>(sp => sp.GetRequiredService<LedgerDbContext>());
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        // Provided cross-module contract — replaces the Accounts no-op default.
        services.AddScoped<ILedgerAccountQuery, LedgerAccountQuery>();

        services.AddScoped<LedgerService>();

        return services;
    }
}
