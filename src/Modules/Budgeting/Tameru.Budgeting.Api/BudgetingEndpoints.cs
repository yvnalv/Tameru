using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Budgeting.Application;
using Tameru.Budgeting.Application.Contracts;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Results;

namespace Tameru.Budgeting.Api;

/// <summary>Maps the Categories, Budget, and Master Plan endpoints (docs/API_SPEC.md).</summary>
public static class BudgetingEndpoints
{
    public static IEndpointRouteBuilder MapBudgetingEndpoints(this IEndpointRouteBuilder app)
    {
        MapCategories(app);
        MapBudget(app);
        MapMasterPlan(app);
        return app;
    }

    private static void MapCategories(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (
            CategoryService service, string? level, string? flow, Guid? parentId, bool? includeInactive,
            CancellationToken ct) =>
            (await service.ListAsync(level, flow, parentId, includeInactive ?? false, ct)).ToHttp());

        group.MapPost("/", async (CreateCategoryRequest request, CategoryService service, CancellationToken ct) =>
            (await service.CreateAsync(request, ct)).ToHttp());

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCategoryRequest request, CategoryService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, request, ct)).ToHttp());

        group.MapPost("/{id:guid}/deactivate", async (Guid id, CategoryService service, CancellationToken ct) =>
            (await service.DeactivateAsync(id, ct)).ToHttp());
    }

    private static void MapBudget(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/budget-periods").WithTags("Budget").RequireAuthorization();

        group.MapGet("/", async (BudgetService service, int? year, CancellationToken ct) =>
            Results.Ok(ApiResponse<IReadOnlyList<BudgetPeriodSummaryDto>>.Ok(
                await service.ListPeriodsAsync(year, ct))));

        group.MapGet("/{year:int}/{month:int}", async (
            int year, int month, BudgetService service, CancellationToken ct) =>
            (await service.GetPeriodAsync(year, month, ct)).ToHttp());

        group.MapPost("/", async (CreateBudgetPeriodRequest request, BudgetService service, CancellationToken ct) =>
            (await service.CreatePeriodAsync(request, ct)).ToHttp());

        group.MapPut("/{id:guid}/lines", async (
            Guid id, UpsertBudgetLinesRequest request, BudgetService service, CancellationToken ct) =>
            (await service.UpsertLinesAsync(id, request, ct)).ToHttp());
    }

    private static void MapMasterPlan(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/master-plan").WithTags("Master Plan").RequireAuthorization();

        group.MapGet("/", async (MasterPlanService service, CancellationToken ct) =>
            Results.Ok(ApiResponse<MasterPlanDto>.Ok(await service.GetAsync(ct))));

        group.MapPost("/items", async (
            CreateMasterPlanItemRequest request, MasterPlanService service, CancellationToken ct) =>
            (await service.CreateItemAsync(request, ct)).ToHttp());

        group.MapPut("/items/{id:guid}", async (
            Guid id, UpdateMasterPlanItemRequest request, MasterPlanService service, CancellationToken ct) =>
            (await service.UpdateItemAsync(id, request, ct)).ToHttp());

        group.MapDelete("/items/{id:guid}", async (Guid id, MasterPlanService service, CancellationToken ct) =>
            (await service.DeleteItemAsync(id, ct)).ToHttp());

        group.MapPut("/sections/{id:guid}", async (
            Guid id, UpdateMasterPlanSectionRequest request, MasterPlanService service, CancellationToken ct) =>
            (await service.UpdateSectionAsync(id, request, ct)).ToHttp());
    }
}
