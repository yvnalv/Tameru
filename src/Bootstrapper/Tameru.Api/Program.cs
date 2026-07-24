using Tameru.Api.Infrastructure;
using Tameru.Application.Abstractions;
using Tameru.SharedKernel.Time;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cross-cutting building blocks. Modules (Identity, Accounts, Ledger, …) register
// their own services here as they are added in later milestones.
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICurrentUser, AnonymousCurrentUser>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

// --- Pipeline ---------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Health check — returns the standard response envelope.
app.MapGet("/health", () => ApiResponse<object>.Ok(new
{
    status = "ok",
    service = "Tameru.Api",
    utc = DateTimeOffset.UtcNow,
}));

app.Run();

/// <summary>Exposed so integration tests can reference the API host via WebApplicationFactory.</summary>
public partial class Program;
