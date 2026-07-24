using Tameru.Application.Abstractions;

namespace Tameru.Api.Infrastructure;

/// <summary>
/// Placeholder <see cref="ICurrentUser"/> used until the Identity module lands (M1). Reports no
/// authenticated owner, so seeding/audit stamping uses <see cref="Guid.Empty"/>.
/// </summary>
public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid UserId => Guid.Empty;

    public bool IsAuthenticated => false;
}
