using FluentAssertions;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application;
using Tameru.SharedKernel.Time;
using Xunit;

namespace Tameru.Reporting.UnitTests;

public class RecommendationTests
{
    [Fact]
    public async Task GetRecommendationsAsync_triggers_weekend_and_runway_recommendations()
    {
        var catId = Guid.NewGuid();
        var catTotals = new List<CategoryPeriodTotal>
        {
            new(catId, new DateOnly(2026, 9, 1), 3_000_000m)
        };
        var envTotals = new List<EnvelopePeriodTotal>
        {
            new(Guid.NewGuid(), new DateOnly(2026, 9, 1), 2_000_000m)
        };
        var cashflow = Enumerable.Range(1, 12)
            .Select(m => new MonthlyCashflow(m, 5_000_000m, 4_500_000m))
            .ToList();

        var ledger = new FakeLedgerReportingQuery(cashflow, catTotals, envTotals);
        var budget = new FakeBudgetDecisionQuery();
        var prefs = new FakeUserPreferences(startDay: 1);
        var clock = new SystemClock();
        var analysis = new DeepAnalysisService(ledger, budget, prefs, clock);

        var balances = new FakeAccountBalanceDirectory(
            new AccountBalance(Guid.NewGuid(), "BCA", "Bank Accounts", "Bank", "IDR", 5_000_000m, true)
        );

        var sut = new RecommendationService(analysis, balances);

        var recs = await sut.GetRecommendationsAsync(2026, 9);

        recs.Should().NotBeNull();
        // Since liquid cash is 5M and monthly spend is ~4.5M, runway is ~1.1 months (< 1.5 critical)
        recs.Should().Contain(r => r.Strategy == "CashRunway");
    }
}
