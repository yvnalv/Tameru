using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tameru.Reporting.Application;

namespace Tameru.Reporting.Infrastructure;

/// <summary>
/// DI registration for the Reporting module. Reporting owns no data and no persistence — it only
/// composes the Accounts and Ledger contracts — so this registers the service and nothing else. The
/// contracts it consumes (<c>IAccountBalanceDirectory</c>, <c>ILedgerReportingQuery</c>) are provided
/// by those modules, so <c>AddReportingModule</c> can be called in any order after them.
/// </summary>
public static class ReportingInfrastructureModule
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ReportingService>();
        return services;
    }
}
