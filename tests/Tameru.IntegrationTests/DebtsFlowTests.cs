using FluentAssertions;

namespace Tameru.IntegrationTests;

/// <summary>
/// End-to-end Debts behaviour through the real API + PostgreSQL: the repayment lifecycle, the
/// derived remaining balance, and the True Net Worth integration
/// (Net Worth = assets - active liabilities) that Reporting reads over the
/// <c>ILiabilityQuery</c> contract rather than by touching the <c>debts</c> schema directly.
/// </summary>
[Collection("api")]
public sealed class DebtsFlowTests
{
    private readonly TameruApiFactory _factory;

    public DebtsFlowTests(TameruApiFactory factory) => _factory = factory;

    private async Task<TestApi> AuthedAsync()
    {
        var api = new TestApi(_factory.CreateClient());
        await api.LoginAsync(TameruApiFactory.OwnerEmail, TameruApiFactory.OwnerPassword);
        return api;
    }

    private static Task<LiabilityData> CreateLiabilityAsync(
        TestApi api, decimal total, decimal initialPaid = 0m, decimal monthly = 0m) =>
        api.PostAsync<LiabilityData>("/api/v1/debts", new
        {
            title = $"KPR-{Guid.NewGuid():N}",
            type = "Installment",
            creditor = "Bank Test",
            totalAmount = total,
            initialPaidAmount = initialPaid,
            monthlyInstallment = monthly,
            dueDay = 5,
            startDate = "2026-01-01",
        });

    [Fact]
    public async Task Remaining_balance_derives_from_total_minus_payments()
    {
        var api = await AuthedAsync();
        var liability = await CreateLiabilityAsync(api, total: 100_000_000m, initialPaid: 10_000_000m);

        liability.RemainingBalance.Should().Be(90_000_000m);

        await api.PostAsync<LiabilityPaymentData>($"/api/v1/debts/{liability.Id}/payments", new
        {
            amount = 15_000_000m,
            date = "2026-02-05",
        });

        var after = await api.GetAsync<LiabilityData>($"/api/v1/debts/{liability.Id}");

        after.PaidAmount.Should().Be(25_000_000m);
        after.RemainingBalance.Should().Be(75_000_000m);
        // 2, not 1: a non-zero InitialPaidAmount is itself stored as an "Initial balance payment"
        // row rather than a bare column, so the opening balance is auditable like any other payment.
        after.PaymentsCount.Should().Be(2);
        after.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Paying_the_full_balance_flips_the_liability_to_paid_off()
    {
        var api = await AuthedAsync();
        var liability = await CreateLiabilityAsync(api, total: 5_000_000m);

        await api.PostAsync<LiabilityPaymentData>($"/api/v1/debts/{liability.Id}/payments", new
        {
            amount = 5_000_000m,
            date = "2026-03-01",
        });

        var after = await api.GetAsync<LiabilityData>($"/api/v1/debts/{liability.Id}");

        after.RemainingBalance.Should().Be(0m);
        after.Status.Should().Be("PaidOff");
        after.ProgressPercentage.Should().Be(100m);
    }

    [Fact]
    public async Task Deleting_a_payment_restores_the_remaining_balance()
    {
        var api = await AuthedAsync();
        var liability = await CreateLiabilityAsync(api, total: 20_000_000m);

        var payment = await api.PostAsync<LiabilityPaymentData>($"/api/v1/debts/{liability.Id}/payments", new
        {
            amount = 7_500_000m,
            date = "2026-04-01",
        });

        await api.DeleteAsync($"/api/v1/debts/{liability.Id}/payments/{payment.Id}");

        var after = await api.GetAsync<LiabilityData>($"/api/v1/debts/{liability.Id}");

        after.PaidAmount.Should().Be(0m);
        after.RemainingBalance.Should().Be(20_000_000m);
        after.Status.Should().Be("Active");
    }

    [Fact]
    public async Task An_active_liability_reduces_reported_net_worth()
    {
        var api = await AuthedAsync();
        var before = await api.GetAsync<NetWorthData>("/api/v1/reports/net-worth");

        await CreateLiabilityAsync(api, total: 30_000_000m, initialPaid: 5_000_000m);

        var after = await api.GetAsync<NetWorthData>("/api/v1/reports/net-worth");

        // Only the outstanding 25,000,000 counts — the repaid portion is no longer owed.
        after.TotalLiabilities.Should().Be(before.TotalLiabilities + 25_000_000m);
        after.TotalAssets.Should().Be(before.TotalAssets, "a liability must not move asset balances");
        after.Total.Should().Be(after.TotalAssets - after.TotalLiabilities);
    }

    [Fact]
    public async Task The_summary_aggregates_outstanding_debt_across_liabilities()
    {
        var api = await AuthedAsync();
        var before = await api.GetAsync<DebtsSummaryData>("/api/v1/debts/summary");

        await CreateLiabilityAsync(api, total: 12_000_000m, initialPaid: 2_000_000m, monthly: 1_000_000m);

        var after = await api.GetAsync<DebtsSummaryData>("/api/v1/debts/summary");

        after.TotalRemaining.Should().Be(before.TotalRemaining + 10_000_000m);
        after.MonthlyCommitment.Should().Be(before.MonthlyCommitment + 1_000_000m);
        after.ActiveCount.Should().Be(before.ActiveCount + 1);
    }
}
