using System.Security.Claims;
using Tameru.Application.Abstractions;

namespace Tameru.Api.Infrastructure;

/// <summary>
/// Resolves the authenticated owner from the current request's JWT claims. When there is no
/// authenticated user (unauthenticated request, background/seed), <see cref="UserId"/> is
/// <see cref="Guid.Empty"/> so audit stamping stays well-defined.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid UserId
    {
        get
        {
            var principal = _accessor.HttpContext?.User;
            var value = principal?.FindFirstValue("sub")
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
