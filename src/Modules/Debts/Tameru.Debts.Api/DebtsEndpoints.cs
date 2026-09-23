using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Debts.Application;
using Tameru.Debts.Application.Contracts;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Results;

namespace Tameru.Debts.Api;

public static class DebtsEndpoints
{
    public static IEndpointRouteBuilder MapDebtsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/debts").WithTags("Debts").RequireAuthorization();

        group.MapGet("/", async (DebtsService service, string? status, string? type, CancellationToken ct) =>
            (await service.ListAsync(status, type, ct)).ToHttp());

        group.MapGet("/summary", async (DebtsService service, CancellationToken ct) =>
            (await service.GetSummaryAsync(ct)).ToHttp());

        group.MapGet("/{id:guid}", async (Guid id, DebtsService service, CancellationToken ct) =>
            (await service.GetByIdAsync(id, ct)).ToHttp());

        group.MapPost("/", async (CreateLiabilityRequest request, DebtsService service, CancellationToken ct) =>
            (await service.CreateAsync(request, ct)).ToHttp());

        group.MapPut("/{id:guid}", async (Guid id, UpdateLiabilityRequest request, DebtsService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, request, ct)).ToHttp());

        group.MapDelete("/{id:guid}", async (Guid id, DebtsService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToHttp());

        group.MapGet("/{id:guid}/payments", async (Guid id, DebtsService service, CancellationToken ct) =>
            (await service.GetPaymentsAsync(id, ct)).ToHttp());

        group.MapPost("/{id:guid}/payments", async (Guid id, RecordLiabilityPaymentRequest request, DebtsService service, CancellationToken ct) =>
            (await service.RecordPaymentAsync(id, request, ct)).ToHttp());

        group.MapDelete("/{id:guid}/payments/{paymentId:guid}", async (Guid id, Guid paymentId, DebtsService service, CancellationToken ct) =>
            (await service.DeletePaymentAsync(id, paymentId, ct)).ToHttp());

        return app;
    }
}
