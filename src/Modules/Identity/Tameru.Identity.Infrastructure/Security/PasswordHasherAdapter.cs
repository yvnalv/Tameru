using Microsoft.AspNetCore.Identity;
using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Domain;

namespace Tameru.Identity.Infrastructure.Security;

/// <summary>
/// Adapts ASP.NET Core's battle-tested <see cref="PasswordHasher{TUser}"/> (PBKDF2) to the
/// module's <see cref="IPasswordHasher"/> port (docs/SECURITY.md).
/// </summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private static readonly User Placeholder = User.Create("placeholder@tameru.local", "x", "placeholder");

    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(Placeholder, password);

    public bool Verify(string passwordHash, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(Placeholder, passwordHash, providedPassword);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
