using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tameru.Debts.Application;
using Tameru.Debts.Application.Abstractions;
using Tameru.Debts.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Debts;

namespace Tameru.Debts.Infrastructure;

public static class DebtsInfrastructureModule
{
    public static IServiceCollection AddDebtsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<DebtsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", DebtsDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IDebtsUnitOfWork>(sp => sp.GetRequiredService<DebtsDbContext>());
        services.AddScoped<ILiabilityRepository, LiabilityRepository>();

        // Provided cross-module contract
        services.AddScoped<ILiabilityQuery, LiabilityQuery>();

        services.AddScoped<DebtsService>();

        return services;
    }
}
