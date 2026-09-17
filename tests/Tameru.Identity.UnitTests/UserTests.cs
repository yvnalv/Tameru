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

        user.UpdateProfile(displayName: null, locale: "id", budgetCycleStartDay: 25);

        user.DisplayName.Should().Be("Yovan");
        user.Locale.Should().Be("id");
        user.BudgetCycleStartDay.Should().Be(25);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    [InlineData(-5)]
    public void BudgetCycleStartDay_out_of_range_throws_domain_rule(int invalidDay)
    {
        var user = User.Create("owner@tameru.local", "hash", "Yovan", "en");

        var act = () => user.UpdateProfile(null, null, invalidDay);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("budget_cycle_start_day_invalid");
    }
}
