using FluentAssertions;
using Tameru.Identity.Domain;
using Xunit;

namespace Tameru.Identity.UnitTests;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_token_is_active_before_expiry()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void Token_is_inactive_after_expiry()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(-1));

        token.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void Revoked_token_is_inactive()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.Revoke(Now);

        token.IsActive(Now).Should().BeFalse();
        token.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public void Revoke_is_idempotent_keeping_first_timestamp()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", Now.AddDays(14));

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        token.RevokedAt.Should().Be(Now);
    }
}
