using FluentAssertions;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application;
using Tameru.SharedKernel.Time;
using Xunit;

namespace Tameru.Reporting.UnitTests;

public class DeepAnalysisTests
{
    private readonly FakeLedgerReportingQuery _ledger;
    private readonly FakeBudgetDecisionQuery _budget;
    private readonly FakeUserPreferences _prefs;
    private readonly IClock _clock;
    private readonly DeepAnalysisService _sut;

    public DeepAnalysisTests()
    {
        var catId = Guid.NewGuid();
        var catTotals = new List<CategoryPeriodTotal>
        {
            new(catId, new DateOnly(2026, 9, 1), 2_500_000m)
        };
        var envTotals = new List<EnvelopePeriodTotal>
        {
            new(Guid.NewGuid(), new DateOnly(2026, 9, 1), 1_500_000m)
        };
        var cashflow = Enumerable.Range(1, 12)
            .Select(m => new MonthlyCashflow(m, m <= 9 ? 10_000_000m : 0m, m <= 9 ? 6_000_000m : 0m))
            .ToList();

        _ledger = new FakeLedgerReportingQuery(cashflow, catTotals, envTotals);
        _budget = new FakeBudgetDecisionQuery();
        _prefs = new FakeUserPreferences(startDay: 1);
        _clock = new SystemClock();

        _sut = new DeepAnalysisService(_ledger, _budget, _prefs, _clock);
    }

    [Fact]
    public async Task GetAnalysisAsync_calculates_retention_rate_and_breakdowns()
    {
        var result = await _sut.GetAnalysisAsync(2026, 9);

        result.Should().NotBeNull();
        result.Period.Year.Should().Be(2026);
        result.Period.Month.Should().Be(9);
        result.Expense.FixedVsVariable.Should().NotBeNull();
        result.Expense.WeekdayVsWeekend.Should().NotBeNull();
        result.Expense.TopPayees.Should().NotBeEmpty();
    }
}
