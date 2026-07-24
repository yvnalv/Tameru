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
using Tameru.Identity.Api;
using Tameru.Identity.Infrastructure;
using Tameru.Identity.Infrastructure.Authentication;
using Tameru.Identity.Infrastructure.Persistence;
using Tameru.Identity.Infrastructure.Seeding;
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

// Modules.
builder.Services.AddIdentityModule(config);
builder.Services.AddAccountsModule(config);

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

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

// --- Startup: migrate + seed ------------------------------------------------
await MigrateAndSeedAsync(app);

// --- Pipeline ---------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => ApiResponse<object>.Ok(new
{
    status = "ok",
    service = "Tameru.Api",
    utc = DateTimeOffset.UtcNow,
}));

app.MapIdentityEndpoints();
app.MapAccountsEndpoints();

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
        await services.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
    }

    if (config.GetValue("Seed:Enabled", false))
    {
        await services.GetRequiredService<IdentitySeeder>().SeedAsync();
        await services.GetRequiredService<AccountsSeeder>().SeedAsync();
    }
}

/// <summary>Exposed so integration tests can reference the API host via WebApplicationFactory.</summary>
public partial class Program;
