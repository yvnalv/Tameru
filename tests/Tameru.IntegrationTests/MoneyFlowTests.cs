using System.Net;
using FluentAssertions;

namespace Tameru.IntegrationTests;

/// <summary>
/// End-to-end money rules through the real API + PostgreSQL: balances and budget actuals are derived
/// from the ledger (ADR-0006), transfers move value between accounts, and category-flow is enforced.
/// </summary>
[Collection("api")]
public sealed class MoneyFlowTests
{
    private readonly TameruApiFactory _factory;

    public MoneyFlowTests(TameruApiFactory factory) => _factory = factory;

    private async Task<TestApi> AuthedAsync()
    {
        var api = new TestApi(_factory.CreateClient());
        await api.LoginAsync(TameruApiFactory.OwnerEmail, TameruApiFactory.OwnerPassword);
        return api;
    }

    private static async Task<CategoryData> CategoryAsync(TestApi api, string name)
    {
        var categories = await api.GetAsync<List<CategoryData>>("/api/v1/categories");
        return categories.Single(c => c.Name == name);
    }

    private static Task<AccountData> CreateAccountAsync(TestApi api, string prefix, decimal opening, string type = "Bank") =>
        api.PostAsync<AccountData>("/api/v1/accounts", new
        {
            name = $"{prefix}-{Guid.NewGuid():N}",
            type,
            openingBalance = opening,
            currencyCode = "IDR",
            sortOrder = 0,
        });

    [Fact]
    public async Task Account_balance_derives_from_the_ledger()
    {
        var api = await AuthedAsync();
        var needs = await CategoryAsync(api, "Needs");
        var food = await CategoryAsync(api, "Food");

        var a = await CreateAccountAsync(api, "A", 5_000_000m);
        var b = await CreateAccountAsync(api, "B", 1_000_000m, "Cash");

        await api.PostAsync<TransactionData>("/api/v1/transactions",
            new { type = "Income", date = "2029-03-10", title = "Salary", amount = 3_000_000m, accountId = a.Id });
        await api.PostAsync<TransactionData>("/api/v1/transactions",
            new { type = "Expense", date = "2029-03-12", title = "Groceries", amount = 500_000m, accountId = a.Id, budgetCategoryId = needs.Id, categoryId = food.Id });
        await api.PostAsync<TransactionData>("/api/v1/transactions",
            new { type = "Transfer", date = "2029-03-15", title = "Move", amount = 2_000_000m, accountId = a.Id, toAccountId = b.Id });

        var aAfter = await api.GetAsync<AccountData>($"/api/v1/accounts/{a.Id}");
        var bAfter = await api.GetAsync<AccountData>($"/api/v1/accounts/{b.Id}");

        aAfter.Balance.Should().Be(5_500_000m); // 5,000,000 + 3,000,000 − 500,000 − 2,000,000
        bAfter.Balance.Should().Be(3_000_000m); // 1,000,000 + 2,000,000
    }

    [Fact]
    public async Task Voiding_a_transaction_recomputes_the_balance()
    {
        var api = await AuthedAsync();
        var account = await CreateAccountAsync(api, "Void", 0m);

        var income = await api.PostAsync<TransactionData>("/api/v1/transactions",
            new { type = "Income", date = "2029-06-01", title = "In", amount = 1_000_000m, accountId = account.Id });
        (await api.GetAsync<AccountData>($"/api/v1/accounts/{account.Id}")).Balance.Should().Be(1_000_000m);

        var voided = await api.Http.PostAsync($"/api/v1/transactions/{income.Id}/void", null);
        voided.StatusCode.Should().Be(HttpStatusCode.OK);

        (await api.GetAsync<AccountData>($"/api/v1/accounts/{account.Id}")).Balance.Should().Be(0m);
    }

    [Fact]
    public async Task Income_with_an_expense_flow_category_is_rejected()
    {
        var api = await AuthedAsync();
        var needs = await CategoryAsync(api, "Needs"); // Expense-flow budget
        var account = await CreateAccountAsync(api, "Flow", 0m);

        var (status, code) = await api.PostExpectFailureAsync("/api/v1/transactions",
            new { type = "Income", date = "2029-04-01", title = "Bad", amount = 1_000m, accountId = account.Id, budgetCategoryId = needs.Id });

        status.Should().Be(HttpStatusCode.UnprocessableEntity);
        code.Should().Be("category_flow_mismatch");
    }

    [Fact]
    public async Task Budget_actual_and_leftover_derive_from_the_ledger()
    {
        var api = await AuthedAsync();
        var needs = await CategoryAsync(api, "Needs");
        var food = await CategoryAsync(api, "Food");
        var account = await CreateAccountAsync(api, "Bud", 10_000_000m);

        // A dedicated period/month so the ledger spend under assertion stays isolated.
        var period = await api.PostAsync<BudgetPeriodData>("/api/v1/budget-periods",
            new { year = 2033, month = 5, note = "test" });
        await api.PutAsync($"/api/v1/budget-periods/{period.Id}/lines",
            new { lines = new[] { new { categoryId = food.Id, planAmount = 1_000_000m } } });

        await api.PostAsync<TransactionData>("/api/v1/transactions",
            new { type = "Expense", date = "2033-05-09", title = "Food spend", amount = 600_000m, accountId = account.Id, budgetCategoryId = needs.Id, categoryId = food.Id });

        var refreshed = await api.GetAsync<BudgetPeriodData>("/api/v1/budget-periods/2033/5");
        var line = refreshed.Lines.Single(l => l.CategoryId == food.Id);

        line.Plan.Should().Be(1_000_000m);
        line.Actual.Should().Be(600_000m);   // derived from the ledger expense
        line.Leftover.Should().Be(400_000m);
    }
}
