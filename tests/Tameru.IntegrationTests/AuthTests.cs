using System.Net;
using FluentAssertions;

namespace Tameru.IntegrationTests;

[Collection("api")]
public sealed class AuthTests
{
    private readonly TameruApiFactory _factory;

    public AuthTests(TameruApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _factory.CreateClient().GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_then_me_returns_the_seeded_owner()
    {
        var api = new TestApi(_factory.CreateClient());
        await api.LoginAsync(TameruApiFactory.OwnerEmail, TameruApiFactory.OwnerPassword);

        var me = await api.GetAsync<UserInfo>("/api/v1/auth/me");

        me.Email.Should().Be(TameruApiFactory.OwnerEmail);
        me.DisplayName.Should().Be("Test Owner");
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var api = new TestApi(_factory.CreateClient());

        var (status, code) = await api.PostExpectFailureAsync(
            "/api/v1/auth/login",
            new { email = TameruApiFactory.OwnerEmail, password = "wrong-password" });

        status.Should().Be(HttpStatusCode.Unauthorized);
        code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task A_protected_endpoint_without_a_token_returns_401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/accounts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
