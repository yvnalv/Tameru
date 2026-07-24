using FluentAssertions;
using Tameru.Identity.Application;
using Tameru.Identity.Application.Contracts;
using Tameru.Identity.Domain;
using Xunit;

namespace Tameru.Identity.UnitTests;

public class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    private readonly TestClock _clock = new(Now);
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeRefreshTokenRepository _refreshTokens = new();
    private readonly FakeIdentityUnitOfWork _uow = new();
    private readonly User _owner;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _owner = User.Create("owner@tameru.local", _hasher.Hash("ChangeMe!123"), "Yovan", "en");
        var users = new FakeUserRepository(_owner);
        var tokens = new FakeTokenService(_clock);
        _sut = new AuthService(users, _refreshTokens, _hasher, tokens, _uow, _clock);
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_tokens_and_user()
    {
        var result = await _sut.LoginAsync(new LoginRequest("owner@tameru.local", "ChangeMe!123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        result.Value.User.Email.Should().Be("owner@tameru.local");
        _refreshTokens.Tokens.Should().ContainSingle();
    }

    [Fact]
    public async Task Login_is_case_insensitive_on_email()
    {
        var result = await _sut.LoginAsync(new LoginRequest("OWNER@Tameru.Local", "ChangeMe!123"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Login_with_wrong_password_fails_with_invalid_credentials()
    {
        var result = await _sut.LoginAsync(new LoginRequest("owner@tameru.local", "nope"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_with_unknown_email_fails_with_invalid_credentials()
    {
        var result = await _sut.LoginAsync(new LoginRequest("ghost@tameru.local", "whatever"));

        result.Error.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_revokes_the_old_one()
    {
        var login = await _sut.LoginAsync(new LoginRequest("owner@tameru.local", "ChangeMe!123"));
        var originalRefresh = login.Value.RefreshToken;

        var refreshed = await _sut.RefreshAsync(new RefreshRequest(originalRefresh));

        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.RefreshToken.Should().NotBe(originalRefresh);
        _refreshTokens.Tokens.Should().HaveCount(2);
        _refreshTokens.Tokens[0].RevokedAt.Should().Be(Now, "the presented token is revoked on rotation");
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_fails()
    {
        var login = await _sut.LoginAsync(new LoginRequest("owner@tameru.local", "ChangeMe!123"));
        var originalRefresh = login.Value.RefreshToken;

        await _sut.RefreshAsync(new RefreshRequest(originalRefresh));
        var reuse = await _sut.RefreshAsync(new RefreshRequest(originalRefresh));

        reuse.IsFailure.Should().BeTrue();
        reuse.Error.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task Refresh_with_unknown_token_fails()
    {
        var result = await _sut.RefreshAsync(new RefreshRequest("does-not-exist"));

        result.Error.Code.Should().Be("invalid_refresh_token");
    }

    [Fact]
    public async Task UpdateMe_changes_profile()
    {
        var result = await _sut.UpdateMeAsync(_owner.Id, new UpdateProfileRequest("Yovan A.", "id"));

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Yovan A.");
        result.Value.Locale.Should().Be("id");
        _uow.SaveCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMe_for_unknown_user_returns_not_found()
    {
        var result = await _sut.GetMeAsync(Guid.NewGuid());

        result.Error.Code.Should().Be("not_found");
    }
}
