using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Application.Contracts;
using Tameru.Identity.Domain;
using Tameru.SharedKernel.Results;
using Tameru.SharedKernel.Time;

namespace Tameru.Identity.Application;

/// <summary>
/// Authentication use cases for the single owner: login, refresh-token rotation, logout, and
/// profile read/update. Business failures are returned as failed <see cref="Result"/>s
/// (docs/ERROR_HANDLING.md), not thrown.
/// </summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IIdentityUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return IdentityErrors.InvalidCredentials;
        }

        var user = await _users.GetByEmailAsync(User.Normalize(request.Email), ct);
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return IdentityErrors.InvalidCredentials;
        }

        var response = await IssueTokensAsync(user, ct);
        return response;
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokens.GetActiveByHashAsync(hash, ct);
        if (stored is null || !stored.IsActive(_clock.UtcNow))
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = await _users.GetByIdAsync(stored.UserId, ct);
        if (user is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        // Rotate: revoke the presented token, then issue a fresh pair.
        stored.Revoke(_clock.UtcNow);
        var response = await IssueTokensAsync(user, ct);
        return response;
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var hash = _tokenService.HashRefreshToken(request.RefreshToken);
            var stored = await _refreshTokens.GetActiveByHashAsync(hash, ct);
            stored?.Revoke(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result<UserDto>> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user is null ? IdentityErrors.UserNotFound : Map(user);
    }

    public async Task<Result<UserDto>> UpdateMeAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        user.UpdateProfile(request.DisplayName, request.Locale);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _tokenService.CreateAccessToken(user);
        var refresh = _tokenService.CreateRefreshToken();

        await _refreshTokens.AddAsync(RefreshToken.Issue(user.Id, refresh.Hash, refresh.ExpiresAt), ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new AuthResponse(access.Token, access.ExpiresAt, refresh.Raw, Map(user));
    }

    private static UserDto Map(User user) => new(user.Id, user.Email, user.DisplayName, user.Locale);
}
