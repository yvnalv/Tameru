using FluentAssertions;
using Tameru.Identity.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Identity.UnitTests;

public class UserTests
{
    [Fact]
    public void Create_normalizes_email_and_defaults_locale()
    {
        var user = User.Create("  Owner@Tameru.Local ", "hash", "Yovan");

        user.Email.Should().Be("owner@tameru.local");
        user.DisplayName.Should().Be("Yovan");
        user.Locale.Should().Be("en");
    }

    [Fact]
    public void Create_uses_email_as_display_name_when_missing()
    {
        var user = User.Create("owner@tameru.local", "hash", "");

        user.DisplayName.Should().Be("owner@tameru.local");
    }

    [Theory]
    [InlineData("id", "id")]
    [InlineData("EN", "en")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    public void Create_coerces_locale_to_supported_value(string input, string expected)
    {
        var user = User.Create("owner@tameru.local", "hash", "Yovan", input);

        user.Locale.Should().Be(expected);
    }

    [Fact]
    public void Create_without_email_throws_domain_rule()
    {
        var act = () => User.Create(" ", "hash", "Yovan");

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("email_required");
    }

    [Fact]
    public void UpdateProfile_changes_only_provided_fields()
    {
        var user = User.Create("owner@tameru.local", "hash", "Yovan", "en");

        user.UpdateProfile(displayName: null, locale: "id");

        user.DisplayName.Should().Be("Yovan");
        user.Locale.Should().Be("id");
    }
}
