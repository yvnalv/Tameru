using FluentAssertions;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Ledger.UnitTests;

public class TransactionTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 25);

    [Fact]
    public void Income_defaults_currency_and_status()
    {
        var t = Transaction.Income(Today, "Salary", 100m, A);

        t.Type.Should().Be(TransactionType.Income);
        t.CurrencyCode.Should().Be("IDR");
        t.Status.Should().Be(TransactionStatus.Uncleared);
        t.ToAccountId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Amount_must_be_positive(decimal amount)
    {
        var act = () => Transaction.Expense(Today, "Food", amount, A);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("amount_not_positive");
    }

    [Fact]
    public void Empty_title_is_rejected()
    {
        var act = () => Transaction.Income(Today, "  ", 100m, A);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("title_required");
    }

    [Fact]
    public void Transfer_to_same_account_is_rejected()
    {
        var act = () => Transaction.Transfer(Today, "Move", 100m, A, A);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("transfer_same_account");
    }

    [Fact]
    public void Transfer_requires_a_target_account()
    {
        var act = () => Transaction.Transfer(Today, "Move", 100m, A, Guid.Empty);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("transfer_target_required");
    }

    [Fact]
    public void Transfer_carries_distinct_accounts()
    {
        var t = Transaction.Transfer(Today, "Move", 100m, A, B);

        t.AccountId.Should().Be(A);
        t.ToAccountId.Should().Be(B);
        t.SignedAmountForSource().Should().Be(-100m);
    }

    [Fact]
    public void Clear_and_unclear_toggle_status_without_touching_amount()
    {
        var t = Transaction.Expense(Today, "Food", 100m, A);

        t.Clear();
        t.Status.Should().Be(TransactionStatus.Cleared);
        t.Amount.Should().Be(100m);

        t.Unclear();
        t.Status.Should().Be(TransactionStatus.Uncleared);
    }

    [Fact]
    public void Reassigning_transfer_to_same_account_is_rejected()
    {
        var t = Transaction.Transfer(Today, "Move", 100m, A, B);

        var act = () => t.ReassignTransfer(A, A);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("transfer_same_account");
    }
}
