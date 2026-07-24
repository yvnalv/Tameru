using FluentAssertions;
using Tameru.Accounts.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Accounts.UnitTests;

public class AccountTests
{
    [Fact]
    public void Create_defaults_to_active_and_functional_currency()
    {
        var account = Account.Create("BCA", AccountType.Bank, openingBalance: 100_000m);

        account.IsActive.Should().BeTrue();
        account.CurrencyCode.Should().Be("IDR");
        account.OpeningBalance.Should().Be(100_000m);
    }

    [Fact]
    public void Create_without_name_throws_domain_rule()
    {
        var act = () => Account.Create(" ", AccountType.Cash);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("account_name_required");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100_000, 25_000)]
    [InlineData(100_000, -30_000)]
    public void BalanceWith_is_opening_plus_movement(decimal opening, decimal movement)
    {
        var account = Account.Create("BCA", AccountType.Bank, opening);

        account.BalanceWith(movement).Should().Be(opening + movement);
    }

    [Fact]
    public void Deactivate_and_activate_toggle_state()
    {
        var account = Account.Create("BCA", AccountType.Bank);

        account.Deactivate();
        account.IsActive.Should().BeFalse();

        account.Activate();
        account.IsActive.Should().BeTrue();
    }
}
