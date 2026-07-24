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
            string? type, Guid? accountId, Guid? categoryId, string? status,
            DateOnly? from, DateOnly? to, string? q, int? page, int? pageSize,
            CancellationToken ct) =>
        {
            var filter = new TransactionFilter(
                type, accountId, categoryId, status, from, to, q, page ?? 1, pageSize ?? 50);
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

        return app;
    }
}
