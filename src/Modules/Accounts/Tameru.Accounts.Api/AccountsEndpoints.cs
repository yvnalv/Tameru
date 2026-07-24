using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Accounts.Application;
using Tameru.Accounts.Application.Contracts;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Results;

namespace Tameru.Accounts.Api;

/// <summary>Maps the <c>/api/v1/accounts</c> and <c>/api/v1/account-groups</c> endpoints.</summary>
public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/v1/accounts").WithTags("Accounts").RequireAuthorization();

        accounts.MapGet("/", async (AccountService service, bool? includeInactive, CancellationToken ct) =>
            Results.Ok(ApiResponse<IReadOnlyList<AccountDto>>.Ok(
                await service.ListAsync(includeInactive ?? false, ct))));

        accounts.MapGet("/{id:guid}", async (Guid id, AccountService service, CancellationToken ct) =>
            (await service.GetAsync(id, ct)).ToHttp());

        accounts.MapPost("/", async (CreateAccountRequest request, AccountService service, CancellationToken ct) =>
            (await service.CreateAsync(request, ct)).ToHttp());

        accounts.MapPut("/{id:guid}", async (
            Guid id, UpdateAccountRequest request, AccountService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, request, ct)).ToHttp());

        accounts.MapPost("/{id:guid}/deactivate", async (Guid id, AccountService service, CancellationToken ct) =>
            (await service.DeactivateAsync(id, ct)).ToHttp());

        var groups = app.MapGroup("/api/v1/account-groups").WithTags("Accounts").RequireAuthorization();

        groups.MapGet("/", async (AccountService service, CancellationToken ct) =>
            Results.Ok(ApiResponse<IReadOnlyList<AccountGroupDto>>.Ok(await service.ListGroupsAsync(ct))));

        groups.MapPost("/", async (
            CreateAccountGroupRequest request, AccountService service, CancellationToken ct) =>
            (await service.CreateGroupAsync(request, ct)).ToHttp());

        groups.MapPut("/{id:guid}", async (
            Guid id, UpdateAccountGroupRequest request, AccountService service, CancellationToken ct) =>
            (await service.UpdateGroupAsync(id, request, ct)).ToHttp());

        return app;
    }
}
