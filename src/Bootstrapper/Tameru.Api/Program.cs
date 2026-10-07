using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Tameru.Accounts.Api;
using Tameru.Accounts.Infrastructure;
using Tameru.Accounts.Infrastructure.Persistence;
using Tameru.Accounts.Infrastructure.Seeding;
using Tameru.Api.Infrastructure;
using Tameru.Application.Abstractions;
using Tameru.Budgeting.Api;
using Tameru.Budgeting.Infrastructure;
using Tameru.Budgeting.Infrastructure.Persistence;
using Tameru.Budgeting.Infrastructure.Seeding;
using Tameru.Debts.Api;
using Tameru.Debts.Infrastructure;
using Tameru.Debts.Infrastructure.Persistence;
using Tameru.Identity.Api;
using Tameru.Identity.Infrastructure;
using Tameru.Identity.Infrastructure.Authentication;
using Tameru.Identity.Infrastructure.Persistence;
using Tameru.Identity.Infrastructure.Seeding;
using Tameru.Ledger.Api;
using Tameru.Ledger.Infrastructure;
using Tameru.Ledger.Infrastructure.Persistence;
using Tameru.Infrastructure.Common.Services;
using Tameru.Reporting.Api;
using Tameru.Reporting.Infrastructure;
using Tameru.SharedKernel.Time;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Middleware;

// Keep JWT claim names as-issued ("sub" stays "sub").
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Services ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(SwaggerWithBearer);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddHttpClient<IChatCompletionService, OpenAiChatService>();

// Modules. Order matters for cross-module contract overrides:
//   Ledger's ILedgerAccountQuery replaces the Accounts no-op;
//   Budgeting's ICategoryDirectory replaces the Ledger no-op.
builder.Services.AddIdentityModule(config);
builder.Services.AddAccountsModule(config);
builder.Services.AddLedgerModule(config);
builder.Services.AddBudgetingModule(config);
builder.Services.AddDebtsModule(config);

// Reporting composes the Accounts + Ledger contracts (read-only); it owns no data.
builder.Services.AddReportingModule(config);

// Authentication / authorization.
var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddTameruRateLimiting(config);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

// --- Startup: migrate + seed ------------------------------------------------
await MigrateAndSeedAsync(app);

// --- Pipeline ---------------------------------------------------------------
// Must run before the rate limiter so it partitions on the real client IP, not the proxy's.
app.UseForwardedHeaders(RateLimiting.BuildForwardedHeadersOptions(config));

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => ApiResponse<object>.Ok(new
{
    status = "ok",
    service = "Tameru.Api",
    utc = DateTimeOffset.UtcNow,
}));

app.MapIdentityEndpoints(RateLimiting.AuthPolicy);
app.MapAccountsEndpoints();
app.MapLedgerEndpoints();
app.MapBudgetingEndpoints();
app.MapDebtsEndpoints();
app.MapReportingEndpoints();
app.MapDecisionEndpoints();
app.MapAssistantEndpoints();

app.Run();
return;

static void SwaggerWithBearer(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tameru API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
}

static async Task MigrateAndSeedAsync(WebApplication app)
{
    var config = app.Configuration;
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    if (config.GetValue("Database:AutoMigrate", false))
    {
        // Each module owns its own schema and migration history, so they are applied one by one.
        // Pending migrations are logged before they run: on a deploy that goes wrong, the log is the
        // record of exactly how far the schema got (docs/DEPLOYMENT.md → Database).
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Tameru.Migrations");

        await MigrateModuleAsync(services.GetRequiredService<IdentityDbContext>(), "Identity", logger);
        await MigrateModuleAsync(services.GetRequiredService<AccountsDbContext>(), "Accounts", logger);
        await MigrateModuleAsync(services.GetRequiredService<LedgerDbContext>(), "Ledger", logger);
        await MigrateModuleAsync(services.GetRequiredService<BudgetingDbContext>(), "Budgeting", logger);
        await MigrateModuleAsync(services.GetRequiredService<DebtsDbContext>(), "Debts", logger);
    }

    if (config.GetValue("Seed:Enabled", false))
    {
        await services.GetRequiredService<IdentitySeeder>().SeedAsync();
        await services.GetRequiredService<AccountsSeeder>().SeedAsync();
        await services.GetRequiredService<BudgetingSeeder>().SeedAsync();
    }
}

/// <summary>
/// Applies one module's pending migrations, naming them in the log first. A failure is rethrown so
/// the container exits rather than serving traffic against a half-migrated schema.
/// </summary>
static async Task MigrateModuleAsync(DbContext db, string module, ILogger logger)
{
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0)
    {
        logger.LogInformation("{Module}: schema up to date", module);
        return;
    }

    logger.LogWarning(
        "{Module}: applying {Count} pending migration(s): {Migrations}",
        module, pending.Count, string.Join(", ", pending));

    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("{Module}: migrated successfully", module);
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "{Module}: migration failed after reaching {Applied}. Restore from backup before retrying.",
            module,
            string.Join(", ", (await db.Database.GetAppliedMigrationsAsync()).TakeLast(1)));
        throw;
    }
}

/// <summary>Exposed so integration tests can reference the API host via WebApplicationFactory.</summary>
public partial class Program;
