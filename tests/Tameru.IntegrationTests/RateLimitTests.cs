using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Tameru.IntegrationTests;

/// <summary>
/// Covers the brute-force guard on the credential endpoints (docs/SECURITY.md → "Rate-limit
/// <c>/auth/login</c>"). Each test spins up its own host via <see cref="WebApplicationFactoryExtensions"/>
/// so it gets a private limiter bucket — draining the shared factory's bucket would make unrelated
/// tests fail with 429.
/// </summary>
[Collection("api")]
public sealed class RateLimitTests
{
    private const int PermitLimit = 3;

    private readonly TameruApiFactory _factory;

    public RateLimitTests(TameruApiFactory factory) => _factory = factory;

    private WebApplicationFactory<Program> LimitedHost() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", PermitLimit.ToString());
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        });

    [Fact]
    public async Task Repeated_failed_logins_are_rate_limited_with_the_documented_envelope()
    {
        using var host = LimitedHost();
        var client = host.CreateClient();
        var body = new { email = TameruApiFactory.OwnerEmail, password = "wrong-password" };

        // The permitted attempts are rejected on credentials, not on the limiter.
        for (var attempt = 1; attempt <= PermitLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/login", body);
            allowed.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized,
                "attempt {0} is within the permit limit of {1}", attempt, PermitLimit);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/login", body);

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.Contains("Retry-After").Should().BeTrue("clients need to know when to retry");

        var envelope = await blocked.Content.ReadFromJsonAsync<Envelope<object>>();
        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeFalse();
        envelope.Error!.Code.Should().Be("rate_limited");
    }

    [Fact]
    public async Task The_refresh_endpoint_shares_the_credential_limit()
    {
        using var host = LimitedHost();
        var client = host.CreateClient();
        var body = new { refreshToken = "not-a-real-token" };

        for (var attempt = 1; attempt <= PermitLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/refresh", body);
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var blocked = await client.PostAsJsonAsync("/api/v1/auth/refresh", body);

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task A_successful_login_is_unaffected_while_within_the_limit()
    {
        using var host = LimitedHost();
        var api = new TestApi(host.CreateClient());

        await api.LoginAsync(TameruApiFactory.OwnerEmail, TameruApiFactory.OwnerPassword);

        var me = await api.GetAsync<UserInfo>("/api/v1/auth/me");
        me.Email.Should().Be(TameruApiFactory.OwnerEmail);
    }
}
