using FluentAssertions;

namespace Tameru.IntegrationTests;

/// <summary>
/// End-to-end Recurring Bills behaviour: paying a bill must both post a real expense into the
/// ledger (so the account balance and budget actuals stay derived from the one source of truth,
/// ADR-0006) and advance the bill to its next billing cycle.
/// </summary>
[Collection("api")]
public sealed class RecurringBillsFlowTests
{
    private readonly TameruApiFactory _factory;

    public RecurringBillsFlowTests(TameruApiFactory factory) => _factory = factory;

    private async Task<TestApi> AuthedAsync()
    {
        var api = new TestApi(_factory.CreateClient());
        await api.LoginAsync(TameruApiFactory.OwnerEmail, TameruApiFactory.OwnerPassword);
        return api;
    }

    private static Task<AccountData> CreateAccountAsync(TestApi api, decimal opening) =>
        api.PostAsync<AccountData>("/api/v1/accounts", new
        {
            name = $"Bills-{Guid.NewGuid():N}",
            type = "Bank",
            openingBalance = opening,
            currencyCode = "IDR",
            sortOrder = 0,
        });

    private static Task<RecurringBillData> CreateBillAsync(
        TestApi api, Guid accountId, decimal amount, string cycle = "Monthly", int dueDay = 10) =>
        api.PostAsync<RecurringBillData>("/api/v1/ledger/recurring-bills", new
        {
            title = $"Netflix-{Guid.NewGuid():N}",
            amount,
            billingCycle = cycle,
            dueDay,
            accountId,
            autoDebit = false,
            remindDaysBefore = 3,
            startDate = "2026-01-01",
        });

    [Fact]
    public async Task Paying_a_bill_posts_an_expense_and_reduces_the_account_balance()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, opening: 10_000_000m);
        var bill = await CreateBillAsync(api, account.Id, amount: 186_000m);

        var result = await api.PostAsync<PayRecurringBillData>(
            $"/api/v1/ledger/recurring-bills/{bill.Id}/pay",
            new { date = "2026-01-10" });

        result.Transaction.Type.Should().Be("Expense");
        result.Transaction.Amount.Should().Be(186_000m);

        var after = await api.GetAsync<AccountData>($"/api/v1/accounts/{account.Id}");
        after.Balance.Should().Be(10_000_000m - 186_000m,
            "the posted expense must flow through the derived balance");
    }

    [Fact]
    public async Task Paying_a_monthly_bill_advances_it_to_the_next_cycle()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, opening: 5_000_000m);
        var bill = await CreateBillAsync(api, account.Id, amount: 100_000m, cycle: "Monthly", dueDay: 10);

        var dueBefore = bill.NextDueDate;

        var result = await api.PostAsync<PayRecurringBillData>(
            $"/api/v1/ledger/recurring-bills/{bill.Id}/pay",
            new { date = "2026-01-10" });

        result.RecurringBill.NextDueDate.Should().BeAfter(dueBefore);
        result.RecurringBill.NextDueDate.Day.Should().Be(10, "the billing day is preserved across cycles");
        result.RecurringBill.LastPaidDate.Should().NotBeNull();
    }

    [Fact]
    public async Task A_yearly_bill_advances_by_a_year_not_a_month()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, opening: 5_000_000m);
        var bill = await CreateBillAsync(api, account.Id, amount: 1_200_000m, cycle: "Yearly", dueDay: 15);

        var result = await api.PostAsync<PayRecurringBillData>(
            $"/api/v1/ledger/recurring-bills/{bill.Id}/pay",
            new { date = "2026-01-15" });

        result.RecurringBill.NextDueDate.Year.Should().Be(bill.NextDueDate.Year + 1);
    }

    [Fact]
    public async Task An_overridden_amount_is_what_reaches_the_ledger()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, opening: 2_000_000m);
        var bill = await CreateBillAsync(api, account.Id, amount: 150_000m);

        // Utility bills vary month to month, so the caller may override the planned amount.
        var result = await api.PostAsync<PayRecurringBillData>(
            $"/api/v1/ledger/recurring-bills/{bill.Id}/pay",
            new { date = "2026-01-10", amount = 212_500m });

        result.Transaction.Amount.Should().Be(212_500m);

        var after = await api.GetAsync<AccountData>($"/api/v1/accounts/{account.Id}");
        after.Balance.Should().Be(2_000_000m - 212_500m);
    }

    [Fact]
    public async Task The_summary_counts_the_monthly_commitment()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, opening: 1_000_000m);
        var before = await api.GetAsync<RecurringSummaryData>("/api/v1/ledger/recurring-bills/summary");

        await CreateBillAsync(api, account.Id, amount: 99_000m);

        var after = await api.GetAsync<RecurringSummaryData>("/api/v1/ledger/recurring-bills/summary");

        after.TotalActiveBills.Should().Be(before.TotalActiveBills + 1);
        after.TotalMonthlyCommitment.Should().Be(before.TotalMonthlyCommitment + 99_000m);
    }
}
