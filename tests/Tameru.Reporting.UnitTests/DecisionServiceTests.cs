using FluentAssertions;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.Reporting.Application;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Time;

namespace Tameru.Reporting.UnitTests;

public sealed class DecisionServiceTests
{
    private sealed class FrozenClock : IClock
    {
        public FrozenClock(DateOnly today) => Today = today;
        public DateTimeOffset UtcNow => Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        public DateOnly Today { get; }
    }

    [Fact]
    public async Task GetSafeToSpend_CalculatesCorrectLiquidityAndDailyAllowance()
    {
        // Arrange
        // Today is Sept 17. Cycle starts on 25th -> current cycle is Aug 25 to Sept 24, next payday Sept 25.
        // Days remaining = 8 days.
        var clock = new FrozenClock(new DateOnly(2026, 9, 17));
        var userPrefs = new FakeUserPreferences(startDay: 25);

        var accounts = new[]
        {
            new AccountBalance(Guid.NewGuid(), "BCA Checking", null, "Bank", "IDR", 10_000_000m, true),
            new AccountBalance(Guid.NewGuid(), "GoPay", null, "EWallet", "IDR", 500_000m, true),
            new AccountBalance(Guid.NewGuid(), "Bibit SBN", null, "Investment", "IDR", 50_000_000m, true), // non-liquid
            new AccountBalance(Guid.NewGuid(), "Old Wallet", null, "Cash", "IDR", 100_000m, false), // inactive
        };

        var obligations = new[]
        {
            // Due Sept 20 (before payday Sept 25)
            new FixedObligationRef(Guid.NewGuid(), "Internet Indihome", 350_000m, "Needs", 20),
            // Due Sept 28 (after payday Sept 25)
            new FixedObligationRef(Guid.NewGuid(), "Rent / Kost", 2_000_000m, "Needs", 28),
        };

        var sut = new DecisionService(
            new FakeAccountBalanceDirectory(accounts),
            userPrefs,
            new FakeBudgetDecisionQuery(obligations: obligations),
            clock);

        // Act
        var result = await sut.GetSafeToSpendAsync();

        // Assert
        // Liquid cash = BCA (10m) + GoPay (0.5m) = 10,500,000
        result.LiquidCash.Should().Be(10_500_000m);
        // Only Internet (350k) is due before payday Sept 25
        result.UnpaidObligations.Should().Be(350_000m);
        // Safety buffer = 5% of 10.5m = 525,000
        result.SafetyBuffer.Should().Be(525_000m);
        // Safe to spend = 10.5m - 350k - 525k = 9,625,000
        result.SafeToSpend.Should().Be(9_625_000m);
        // Days remaining = Sept 25 - Sept 17 = 8
        result.DaysRemaining.Should().Be(8);
        // Daily allowance = 9,625,000 / 8 = 1,203,125
        result.DailyAllowance.Should().Be(1_203_125m);
        result.NextPayday.Should().Be(new DateOnly(2026, 9, 25));
        result.LiquidAccounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task SimulatePurchase_ReturnsSafe_WhenWithinBudgetAndLiquidity()
    {
        var clock = new FrozenClock(new DateOnly(2026, 9, 17));
        var userPrefs = new FakeUserPreferences(startDay: 25);
        var catId = Guid.NewGuid();

        var accounts = new[]
        {
            new AccountBalance(Guid.NewGuid(), "BCA", null, "Bank", "IDR", 5_000_000m, true)
        };

        var categories = new[]
        {
            new CategoryBudgetStatus(catId, "Dining Out", 1_000_000m, 200_000m, 800_000m)
        };

        var sut = new DecisionService(
            new FakeAccountBalanceDirectory(accounts),
            userPrefs,
            new FakeBudgetDecisionQuery(categories: categories),
            clock);

        var request = new SimulatePurchaseRequest(150_000m, catId, "Dinner");

        // Act
        var result = await sut.SimulatePurchaseAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Verdict.Should().Be("Safe");
        result.Value.CategoryLeftoverAfterPurchase.Should().Be(650_000m);
        result.Value.NewDailyAllowance.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SimulatePurchase_ReturnsWarningWithSurplus_WhenExceedsCategoryLeftover()
    {
        var clock = new FrozenClock(new DateOnly(2026, 9, 17));
        var userPrefs = new FakeUserPreferences(startDay: 25);
        var diningCatId = Guid.NewGuid();
        var hobbiesCatId = Guid.NewGuid();

        var accounts = new[]
        {
            new AccountBalance(Guid.NewGuid(), "BCA", null, "Bank", "IDR", 5_000_000m, true)
        };

        var categories = new[]
        {
            new CategoryBudgetStatus(diningCatId, "Dining Out", 500_000m, 400_000m, 100_000m),
            new CategoryBudgetStatus(hobbiesCatId, "Hobbies", 1_000_000m, 200_000m, 800_000m)
        };

        var sut = new DecisionService(
            new FakeAccountBalanceDirectory(accounts),
            userPrefs,
            new FakeBudgetDecisionQuery(categories: categories),
            clock);

        var request = new SimulatePurchaseRequest(250_000m, diningCatId, "Fancy steak dinner");

        // Act
        var result = await sut.SimulatePurchaseAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Verdict.Should().Be("Warning");
        result.Value.SurplusCategories.Should().ContainSingle(c => c.CategoryId == hobbiesCatId && c.AvailableSurplus == 800_000m);
    }

    [Fact]
    public async Task SimulatePurchase_ReturnsRisky_WhenExceedsSafeToSpend()
    {
        var clock = new FrozenClock(new DateOnly(2026, 9, 17));
        var userPrefs = new FakeUserPreferences(startDay: 25);

        var accounts = new[]
        {
            new AccountBalance(Guid.NewGuid(), "BCA", null, "Bank", "IDR", 1_000_000m, true)
        };

        var sut = new DecisionService(
            new FakeAccountBalanceDirectory(accounts),
            userPrefs,
            new FakeBudgetDecisionQuery(),
            clock);

        var request = new SimulatePurchaseRequest(2_500_000m, null, "New Smartphone");

        // Act
        var result = await sut.SimulatePurchaseAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Verdict.Should().Be("Risky");
        result.Value.NewSafeToSpend.Should().Be(0m);
    }

    [Fact]
    public async Task SimulatePurchase_RejectsZeroOrNegativeAmount()
    {
        var clock = new FrozenClock(new DateOnly(2026, 9, 17));
        var sut = new DecisionService(
            new FakeAccountBalanceDirectory(),
            new FakeUserPreferences(),
            new FakeBudgetDecisionQuery(),
            clock);

        var result = await sut.SimulatePurchaseAsync(new SimulatePurchaseRequest(0m));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("simulator_amount_positive");
    }

    [Theory]
    [InlineData("2026-09-17", 25, "2026-08-25", "2026-09-24", "2026-09-25", 8)]
    [InlineData("2026-09-25", 25, "2026-09-25", "2026-10-24", "2026-10-25", 30)]
    [InlineData("2026-09-17", 1, "2026-09-01", "2026-09-30", "2026-10-01", 14)]
    public void CalculateCycle_HandlesAnchorDatesCorrectly(
        string todayStr, int startDay, string expectedStart, string expectedEnd, string expectedPayday, int expectedDays)
    {
        var today = DateOnly.Parse(todayStr);
        var (start, end, payday, days) = DecisionService.CalculateCycle(today, startDay);

        start.Should().Be(DateOnly.Parse(expectedStart));
        end.Should().Be(DateOnly.Parse(expectedEnd));
        payday.Should().Be(DateOnly.Parse(expectedPayday));
        days.Should().Be(expectedDays);
    }
}
