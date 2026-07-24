using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tameru.Identity.Application;
using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Infrastructure.Authentication;
using Tameru.Identity.Infrastructure.Persistence;
using Tameru.Identity.Infrastructure.Security;
using Tameru.Identity.Infrastructure.Seeding;

namespace Tameru.Identity.Infrastructure;

/// <summary>DI registration for the Identity module's infrastructure and application services.</summary>
public static class IdentityInfrastructureModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OwnerSeedOptions>(configuration.GetSection(OwnerSeedOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<AuthService>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }
}
