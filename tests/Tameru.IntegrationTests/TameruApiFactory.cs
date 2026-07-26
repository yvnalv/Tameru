using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tameru.IntegrationTests;

/// <summary>
/// Boots the real API host against a throwaway PostgreSQL container (Testcontainers). The app
/// auto-migrates every module and seeds the owner + taxonomy on startup, so tests exercise the full
/// stack — endpoints, EF Core, and the cross-module contracts — against a real database.
/// </summary>
public sealed class TameruApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string OwnerEmail = "owner@test.local";
    public const string OwnerPassword = "Test!12345";

    private readonly Testcontainers.PostgreSql.PostgreSqlContainer _db =
        new Testcontainers.PostgreSql.PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();

    // Environment variables win over the (empty) appsettings connection string, which the minimal-host
    // WebApplicationFactory's ConfigureAppConfiguration would not reliably override.
    private static readonly string[] Keys =
    [
        "ConnectionStrings__Postgres", "Database__AutoMigrate", "Seed__Enabled",
        "Seed__Owner__Email", "Seed__Owner__Password", "Seed__Owner__DisplayName", "Seed__Owner__Locale",
        "Jwt__SigningKey", "Jwt__Issuer", "Jwt__Audience", "Cors__AllowedOrigins__0",
    ];

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Database__AutoMigrate", "true");
        Environment.SetEnvironmentVariable("Seed__Enabled", "true");
        Environment.SetEnvironmentVariable("Seed__Owner__Email", OwnerEmail);
        Environment.SetEnvironmentVariable("Seed__Owner__Password", OwnerPassword);
        Environment.SetEnvironmentVariable("Seed__Owner__DisplayName", "Test Owner");
        Environment.SetEnvironmentVariable("Seed__Owner__Locale", "en");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-tests-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "tameru");
        Environment.SetEnvironmentVariable("Jwt__Audience", "tameru");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _db.DisposeAsync();
        foreach (var key in Keys) Environment.SetEnvironmentVariable(key, null);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<TameruApiFactory>;
