using Tameru.Identity.Domain;

namespace Tameru.Identity.Application.Abstractions;

/// <summary>Issues JWT access tokens and opaque refresh tokens. Implemented in Infrastructure.</summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    /// <summary>Creates a new opaque refresh token: the raw value (returned once), its stored hash,
    /// and its expiry.</summary>
    RefreshTokenValue CreateRefreshToken();

    /// <summary>Hashes a raw refresh token for lookup/comparison against the stored hash.</summary>
    string HashRefreshToken(string rawToken);
}

/// <summary>A signed JWT access token and its absolute expiry (UTC).</summary>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>A freshly minted refresh token: raw value, its hash, and expiry.</summary>
public sealed record RefreshTokenValue(string Raw, string Hash, DateTimeOffset ExpiresAt);
