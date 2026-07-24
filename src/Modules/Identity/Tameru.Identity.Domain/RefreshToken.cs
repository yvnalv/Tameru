using Tameru.SharedKernel.Domain;

namespace Tameru.Identity.Domain;

/// <summary>
/// A refresh token issued to the owner. Only a hash of the token is stored (docs/SECURITY.md).
/// Rotation: refreshing revokes the presented token and issues a new one.
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    /// <summary>Hash of the opaque refresh token; the raw token is never persisted.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTimeOffset expiresAt) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAt);

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
