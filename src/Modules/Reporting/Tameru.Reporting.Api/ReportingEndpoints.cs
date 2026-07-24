using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Reporting.Application;
using Tameru.Web.Common.Results;

namespace Tameru.Reporting.Api;

/// <summary>Maps the <c>/api/v1/reports</c> endpoints (docs/API_SPEC.md → Reporting).</summary>
public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Reporting").RequireAuthorization();

        group.MapGet("/net-worth", async (ReportingService service, CancellationToken ct) =>
            (await service.GetNetWorthAsync(ct)).ToHttp());

        group.MapGet("/cashflow", async (
            int? year, int? month, ReportingService service, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return (await service.GetCashflowAsync(year ?? today.Year, month ?? today.Month, ct)).ToHttp();
        });

        group.MapGet("/overview", async (int? year, ReportingService service, CancellationToken ct) =>
        {
            var currentYear = DateTime.UtcNow.Year;
            return (await service.GetOverviewAsync(year ?? currentYear, ct)).ToHttp();
        });

        group.MapGet("/category-tracker", async (
            string? granularity, DateOnly? from, DateOnly? to, ReportingService service, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var start = from ?? new DateOnly(today.Year, today.Month, 1);
            var end = to ?? today;
            return (await service.GetCategoryTrackerAsync(granularity, start, end, ct)).ToHttp();
        });

        return app;
    }
}
