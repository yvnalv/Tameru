namespace Tameru.Identity.Application.Contracts;

/// <summary>Login with the owner's email and password.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Exchange a valid refresh token for a new token pair (rotation).</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Revoke a refresh token.</summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>Update the owner's display name and/or locale.</summary>
public sealed record UpdateProfileRequest(string? DisplayName, string? Locale);

/// <summary>The authenticated owner, as returned to the client.</summary>
public sealed record UserDto(Guid Id, string Email, string DisplayName, string Locale);

/// <summary>A successful authentication: access token (+ expiry), refresh token, and the user.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserDto User);
