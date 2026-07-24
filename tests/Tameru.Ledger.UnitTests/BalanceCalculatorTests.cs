using FluentAssertions;
using Tameru.Ledger.Domain;
using Xunit;

namespace Tameru.Ledger.UnitTests;

/// <summary>
/// Highest-priority coverage: balance derivation (docs/TESTING.md). These pin the money rules that
/// every balance/report depends on (ADR-0006).
/// </summary>
public class BalanceCalculatorTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly DateOnly D1 = new(2026, 6, 1);
    private static readonly DateOnly D2 = new(2026, 6, 15);
    private static readonly DateOnly D3 = new(2026, 6, 30);

    [Fact]
    public void No_transactions_yields_opening_balance()
    {
        BalanceCalculator.Balance(A, 100_000m, Array.Empty<Transaction>()).Should().Be(100_000m);
    }

    [Fact]
    public void Income_adds_and_expense_subtracts()
    {
        var txns = new[]
        {
            Transaction.Income(D1, "Salary", 15_000_000m, A),
            Transaction.Expense(D2, "Food", 2_000_000m, A),
        };

        BalanceCalculator.Balance(A, 0m, txns).Should().Be(13_000_000m);
    }

    [Fact]
    public void Transfer_moves_between_source_and_destination()
    {
        var txns = new[] { Transaction.Transfer(D1, "To savings", 7_300_000m, A, B) };

        BalanceCalculator.NetMovement(A, txns).Should().Be(-7_300_000m);
        BalanceCalculator.NetMovement(B, txns).Should().Be(7_300_000m);
    }

    [Fact]
    public void Net_movement_ignores_unrelated_accounts()
    {
        var txns = new[] { Transaction.Income(D1, "Salary", 1_000m, B) };

        BalanceCalculator.NetMovement(A, txns).Should().Be(0m);
    }

    [Fact]
    public void AsOf_excludes_later_transactions()
    {
        var txns = new[]
        {
            Transaction.Income(D1, "Salary", 1_000m, A),
            Transaction.Income(D3, "Bonus", 500m, A),
        };

        BalanceCalculator.NetMovement(A, txns, asOf: D2).Should().Be(1_000m);
        BalanceCalculator.NetMovement(A, txns, asOf: D3).Should().Be(1_500m);
    }

    [Fact]
    public void Fractional_amounts_are_preserved()
    {
        var txns = new[] { Transaction.Income(D1, "Interest", 9_586.48m, A) };

        BalanceCalculator.Balance(A, 0m, txns).Should().Be(9_586.48m);
    }

    [Fact]
    public void Combined_flows_compute_correctly()
    {
        var txns = new[]
        {
            Transaction.Income(D1, "Salary", 15_000_000m, A),
            Transaction.Expense(D1, "Rent", 3_000_000m, A),
            Transaction.Transfer(D2, "Move", 5_000_000m, A, B),
            Transaction.Income(D2, "Gift", 1_000_000m, B),
        };

        BalanceCalculator.Balance(A, 100_000m, txns).Should().Be(7_100_000m);
        BalanceCalculator.Balance(B, 0m, txns).Should().Be(6_000_000m);
    }
}
