using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Reporting.Application;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Results;
using Tameru.Web.Common.Results;

namespace Tameru.Reporting.Api;

public static class DecisionEndpoints
{
    public static IEndpointRouteBuilder MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/decision").WithTags("Decision Support").RequireAuthorization();

        group.MapGet("/safe-to-spend", async (DecisionService service, CancellationToken ct) =>
        {
            var result = await service.GetSafeToSpendAsync(ct);
            return Result<SafeToSpendDto>.Success(result).ToHttp();
        });

        group.MapPost("/simulate-purchase", async (
            SimulatePurchaseRequest request, DecisionService service, CancellationToken ct) =>
        {
            var result = await service.SimulatePurchaseAsync(request, ct);
            return result.ToHttp();
        });

        group.MapGet("/insights", async (
            InsightsService insights, int? maxResults, string? minSeverity, CancellationToken ct) =>
        {
            var request = new InsightsRequest(maxResults, minSeverity);
            var result = await insights.GetInsightsAsync(request, ct);
            return Result<IReadOnlyList<InsightDto>>.Success(result).ToHttp();
        });

        return app;
    }
}
