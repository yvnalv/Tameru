using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tameru.Budgeting.Application;
using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Infrastructure.Persistence;
using Tameru.Budgeting.Infrastructure.Seeding;
using Tameru.Modules.Contracts.Budgeting;

namespace Tameru.Budgeting.Infrastructure;

/// <summary>DI registration for the Budgeting module.</summary>
public static class BudgetingInfrastructureModule
{
    public static IServiceCollection AddBudgetingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<BudgetingDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", BudgetingDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IBudgetingUnitOfWork>(sp => sp.GetRequiredService<BudgetingDbContext>());
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IMasterPlanRepository, MasterPlanRepository>();

        // Provided cross-module contract (Ledger validates categories through this).
        services.AddScoped<ICategoryDirectory, CategoryDirectory>();

        services.AddScoped<CategoryService>();
        services.AddScoped<BudgetService>();
        services.AddScoped<MasterPlanService>();
        services.AddScoped<BudgetingSeeder>();

        return services;
    }
}
