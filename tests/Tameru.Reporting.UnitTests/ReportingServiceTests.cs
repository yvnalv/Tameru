using FluentAssertions;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application;

namespace Tameru.Reporting.UnitTests;

public class ReportingServiceTests
{
    private static ReportingService Build(
        FakeAccountBalanceDirectory? accounts = null, FakeLedgerReportingQuery? ledger = null) =>
        new(accounts ?? new FakeAccountBalanceDirectory(), ledger ?? new FakeLedgerReportingQuery());

    private static AccountBalance Account(string name, decimal balance, bool active = true) =>
        new(Guid.NewGuid(), name, "Cash", "Bank", "IDR", balance, active);

    // --- Net worth ----------------------------------------------------------

    [Fact]
    public async Task NetWorth_sums_active_account_balances_only()
    {
        // The directory is asked for active-only, but assert the service does not count inactive rows.
        var accounts = new FakeAccountBalanceDirectory(
            Account("Cash", 1_000_000m),
            Account("Bank", 2_500_000m),
            Account("Closed", 9_000_000m, active: false));

        var result = await Build(accounts: accounts).GetNetWorthAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(3_500_000m);
        result.Value.CurrencyCode.Should().Be("IDR");
        result.Value.Accounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task NetWorth_is_zero_when_no_accounts()
    {
        var result = await Build().GetNetWorthAsync();

        result.Value.Total.Should().Be(0m);
        result.Value.Accounts.Should().BeEmpty();
    }

    // --- Cashflow -----------------------------------------------------------

    [Fact]
    public async Task Cashflow_selects_the_month_and_returns_the_full_year_trend()
    {
        var months = Enumerable.Range(1, 12)
            .Select(m => new MonthlyCashflow(m, Income: m * 100m, Expense: m * 40m))
            .ToList();
        var service = Build(ledger: new FakeLedgerReportingQuery(cashflow: months));

        var result = await service.GetCashflowAsync(2026, 3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Income.Should().Be(300m);
        result.Value.Expense.Should().Be(120m);
        result.Value.Net.Should().Be(180m);
        result.Value.Trend.Should().HaveCount(12);
        result.Value.Trend[11].Net.Should().Be(1_200m - 480m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Cashflow_rejects_an_out_of_range_month(int month)
    {
        var result = await Build().GetCashflowAsync(2026, month);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    // --- Overview -----------------------------------------------------------

    [Fact]
    public async Task Overview_pivots_categories_into_twelve_month_columns()
    {
        var food = Guid.NewGuid();
        var fuel = Guid.NewGuid();
        var totals = new List<CategoryPeriodTotal>
        {
            new(food, new DateOnly(2026, 1, 1), 500_000m),
            new(food, new DateOnly(2026, 3, 1), 300_000m),
            new(fuel, new DateOnly(2026, 1, 1), 200_000m),
        };
        var service = Build(ledger: new FakeLedgerReportingQuery(categoryTotals: totals));

        var result = await service.GetOverviewAsync(2026);

        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(1_000_000m);
        result.Value.MonthlyTotals.Should().HaveCount(12);
        result.Value.MonthlyTotals[0].Should().Be(700_000m); // Jan: food 500k + fuel 200k
        result.Value.MonthlyTotals[2].Should().Be(300_000m); // Mar: food 300k

        // Rows ordered by total descending: food (800k) before fuel (200k).
        result.Value.Categories.Should().HaveCount(2);
        result.Value.Categories[0].CategoryId.Should().Be(food);
        result.Value.Categories[0].Months[0].Should().Be(500_000m);
        result.Value.Categories[0].Months[2].Should().Be(300_000m);
        result.Value.Categories[0].Total.Should().Be(800_000m);
    }

    // --- Category tracker ---------------------------------------------------

    [Fact]
    public async Task Tracker_monthly_builds_period_columns_only_where_spend_exists()
    {
        var food = Guid.NewGuid();
        var totals = new List<CategoryPeriodTotal>
        {
            new(food, new DateOnly(2026, 1, 1), 100_000m),
            new(food, new DateOnly(2026, 4, 1), 250_000m),
        };
        var service = Build(ledger: new FakeLedgerReportingQuery(categoryTotals: totals));

        var result = await service.GetCategoryTrackerAsync(
            "monthly", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        result.IsSuccess.Should().BeTrue();
        result.Value.Periods.Should().Equal(new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1));
        result.Value.Categories.Should().ContainSingle();
        result.Value.Categories[0].Amounts.Should().Equal(100_000m, 250_000m);
        result.Value.PeriodTotals.Should().Equal(100_000m, 250_000m);
        result.Value.Total.Should().Be(350_000m);
    }

    [Fact]
    public async Task Tracker_defaults_to_monthly_when_granularity_is_omitted()
    {
        var result = await Build().GetCategoryTrackerAsync(
            null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        result.IsSuccess.Should().BeTrue();
        result.Value.Granularity.Should().Be("Monthly");
    }

    [Fact]
    public async Task Tracker_rejects_an_unknown_granularity()
    {
        var result = await Build().GetCategoryTrackerAsync(
            "weekly", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    [Fact]
    public async Task Tracker_rejects_an_inverted_date_range()
    {
        var result = await Build().GetCategoryTrackerAsync(
            "daily", new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }
}
