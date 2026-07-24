using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tameru.Identity.Application;
using Tameru.Identity.Application.Contracts;
using Tameru.SharedKernel.Results;
using Tameru.Web.Common.Contracts;
using Tameru.Web.Common.Results;

namespace Tameru.Identity.Api;

/// <summary>Maps the <c>/api/v1/auth</c> endpoints (docs/API_SPEC.md → Auth).</summary>
public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.LoginAsync(request, ct)).ToHttp())
            .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.RefreshAsync(request, ct)).ToHttp())
            .AllowAnonymous();

        group.MapPost("/logout", async (LogoutRequest request, AuthService auth, CancellationToken ct) =>
            (await auth.LogoutAsync(request, ct)).ToHttp())
            .AllowAnonymous();

        group.MapGet("/me", async (ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Unauthenticated();
            }

            return (await auth.GetMeAsync(userId, ct)).ToHttp();
        }).RequireAuthorization();

        group.MapPatch("/me", async (
            UpdateProfileRequest request, ClaimsPrincipal principal, AuthService auth, CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Unauthenticated();
            }

            return (await auth.UpdateMeAsync(userId, request, ct)).ToHttp();
        }).RequireAuthorization();

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private static IResult Unauthenticated() =>
        Results.Json(
            ApiResponse.Fail("Not authenticated.", new ApiError { Code = "unauthenticated" }),
            statusCode: StatusCodes.Status401Unauthorized);
}
