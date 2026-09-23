using FluentAssertions;
using Tameru.Ledger.Domain;
using Xunit;

namespace Tameru.Ledger.UnitTests;

public class RecurringBillTests
{
    [Fact]
    public void CalculateInitialDueDate_picks_current_month_if_due_day_is_in_future()
    {
        var fromDate = new DateOnly(2026, 9, 10);
        var initial = RecurringBill.CalculateInitialDueDate(fromDate, BillingCycle.Monthly, 20);

        initial.Should().Be(new DateOnly(2026, 9, 20));
    }

    [Fact]
    public void CalculateInitialDueDate_picks_next_month_if_due_day_has_passed()
    {
        var fromDate = new DateOnly(2026, 9, 25);
        var initial = RecurringBill.CalculateInitialDueDate(fromDate, BillingCycle.Monthly, 10);

        initial.Should().Be(new DateOnly(2026, 10, 10));
    }

    [Fact]
    public void RecordPayment_advances_monthly_due_date_by_one_month()
    {
        var bill = RecurringBill.Create(
            title: "Netflix",
            amount: 186_000m,
            billingCycle: BillingCycle.Monthly,
            dueDay: 15,
            accountId: Guid.NewGuid(),
            startDate: new DateOnly(2026, 9, 1));

        bill.NextDueDate.Should().Be(new DateOnly(2026, 9, 15));

        bill.RecordPayment(new DateOnly(2026, 9, 14));

        bill.LastPaidDate.Should().Be(new DateOnly(2026, 9, 14));
        bill.NextDueDate.Should().Be(new DateOnly(2026, 10, 15));
    }

    [Fact]
    public void RecordPayment_advances_quarterly_due_date_by_three_months()
    {
        var bill = RecurringBill.Create(
            title: "Internet Provider",
            amount: 1_200_000m,
            billingCycle: BillingCycle.Quarterly,
            dueDay: 5,
            accountId: Guid.NewGuid(),
            startDate: new DateOnly(2026, 1, 1));

        bill.RecordPayment(new DateOnly(2026, 1, 5));

        bill.NextDueDate.Should().Be(new DateOnly(2026, 4, 5));
    }
}
