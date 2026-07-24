using Tameru.SharedKernel.Results;

namespace Tameru.Identity.Application;

/// <summary>Stable Identity error codes (docs/ERROR_HANDLING.md).</summary>
public static class IdentityErrors
{
    public static readonly Error InvalidCredentials =
        new("invalid_credentials", "Email or password is incorrect.");

    public static readonly Error InvalidRefreshToken =
        new("invalid_refresh_token", "The refresh token is invalid or expired.");

    public static readonly Error UserNotFound =
        new("not_found", "User not found.");
}
