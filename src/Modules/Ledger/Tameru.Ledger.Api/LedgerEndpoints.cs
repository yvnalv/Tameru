using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Ledger.Application;
using Tameru.Ledger.Application.Contracts;
using Tameru.Web.Common.Results;

namespace Tameru.Ledger.Api;

/// <summary>Maps the <c>/api/v1/transactions</c> endpoints (docs/API_SPEC.md → Transactions).</summary>
public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions").WithTags("Transactions").RequireAuthorization();

        group.MapGet("/", async (
            LedgerService service,
            string? type, Guid? accountId, Guid? budgetCategoryId, Guid? categoryId, string? status,
            DateOnly? from, DateOnly? to, string? q, int? page, int? pageSize,
            CancellationToken ct) =>
        {
            var filter = new TransactionFilter(
                type, accountId, budgetCategoryId, categoryId, status, from, to, q, page ?? 1, pageSize ?? 50);
            return (await service.ListAsync(filter, ct)).ToHttp();
        });

        group.MapGet("/{id:guid}", async (Guid id, LedgerService service, CancellationToken ct) =>
            (await service.GetAsync(id, ct)).ToHttp());

        group.MapPost("/", async (CreateTransactionRequest request, LedgerService service, CancellationToken ct) =>
            (await service.CreateAsync(request, ct)).ToHttp());

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateTransactionRequest request, LedgerService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, request, ct)).ToHttp());

        group.MapPost("/{id:guid}/clear", async (Guid id, LedgerService service, CancellationToken ct) =>
            (await service.ClearAsync(id, ct)).ToHttp());

        group.MapPost("/{id:guid}/unclear", async (Guid id, LedgerService service, CancellationToken ct) =>
            (await service.UnclearAsync(id, ct)).ToHttp());

        group.MapPost("/{id:guid}/void", async (Guid id, LedgerService service, CancellationToken ct) =>
            (await service.VoidAsync(id, ct)).ToHttp());

        // --- Rules Management -----------------------------------------------
        var rulesGroup = app.MapGroup("/api/v1/rules").WithTags("Rules").RequireAuthorization();

        rulesGroup.MapGet("/", async (RuleService service, bool? activeOnly, CancellationToken ct) =>
            (await service.ListAsync(activeOnly ?? false, ct)).ToHttp());

        rulesGroup.MapPost("/", async (CreateRuleRequest request, RuleService service, CancellationToken ct) =>
            (await service.CreateAsync(request, ct)).ToHttp());

        rulesGroup.MapPut("/{id:guid}", async (Guid id, UpdateRuleRequest request, RuleService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, request, ct)).ToHttp());

        rulesGroup.MapPost("/dry-run", async (DryRunRuleRequest request, RuleService service, CancellationToken ct) =>
            (await service.DryRunAsync(request, ct)).ToHttp());

        rulesGroup.MapGet("/audit-log", async (RuleService service, Guid? ruleId, int? limit, CancellationToken ct) =>
            (await service.ListAuditLogAsync(ruleId, limit ?? 50, ct)).ToHttp());

        rulesGroup.MapGet("/templates", async (RuleService service, CancellationToken ct) =>
            (await service.ListTemplatesAsync(ct)).ToHttp());

        rulesGroup.MapPost("/from-template/{id:guid}", async (Guid id, CreateFromTemplateRequest request, RuleService service, CancellationToken ct) =>
            (await service.CreateFromTemplateAsync(id, request, ct)).ToHttp());

        rulesGroup.MapDelete("/{id:guid}", async (Guid id, RuleService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToHttp());

        // --- Ingestion Webhook / Quick-Log -----------------------------------
        app.MapPost("/api/v1/ingest/transaction", async (
            IngestTransactionRequest request,
            HttpContext httpContext,
            IngestionService ingestion,
            Tameru.Modules.Contracts.Identity.IApiTokenValidator tokenValidator,
            CancellationToken ct) =>
        {
            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;
            if (!isAuthenticated)
            {
                var tokenHeader = httpContext.Request.Headers["X-Tameru-Token"].ToString();
                if (string.IsNullOrWhiteSpace(tokenHeader) || !await tokenValidator.ValidateAsync(tokenHeader, ct))
                {
                    return Results.Json(
                        Tameru.Web.Common.Contracts.ApiResponse.Fail(
                            "Unauthorized ingestion token.", new Tameru.Web.Common.Contracts.ApiError { Code = "unauthorized" }),
                        statusCode: StatusCodes.Status401Unauthorized);
                }
            }

            return (await ingestion.IngestAsync(request, ct)).ToHttp();
        }).WithTags("Ingestion").AllowAnonymous();

        return app;
    }
}
