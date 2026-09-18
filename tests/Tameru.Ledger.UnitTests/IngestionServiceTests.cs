using FluentAssertions;
using Tameru.Ledger.Application;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.SharedKernel.Time;
using Xunit;

namespace Tameru.Ledger.UnitTests;

public class IngestionServiceTests
{
    private static readonly Guid AccountBca = Guid.NewGuid();
    private static readonly Guid AccountGopay = Guid.NewGuid();
    private static readonly Guid CategoryCoffee = Guid.NewGuid();

    private readonly List<AccountRef> _accounts = new()
    {
        new AccountRef(AccountBca, "BCA", "Bank", "IDR"),
        new AccountRef(AccountGopay, "GoPay", "Wallet", "IDR")
    };

    [Theory]
    [InlineData("kopi kenangan 35k gopay", 35000, "Expense", "Kopi Kenangan")]
    [InlineData("makan siang 45.000 bca", 45000, "Expense", "Makan Siang")]
    [InlineData("listrik 250rb via bca", 250000, "Expense", "Listrik")]
    [InlineData("gaji bulanan 15jt bca", 15000000, "Income", "Gaji Bulanan")]
    [InlineData("grab ride 25k", 25000, "Expense", "Grab Ride")]
    public void ParseNaturalText_extracts_fields_correctly(string input, decimal expectedAmount, string expectedType, string expectedTitlePrefix)
    {
        var result = IngestionService.ParseNaturalText(input, _accounts);

        result.Amount.Should().Be(expectedAmount);
        result.Type.Should().Be(expectedType);
        result.Title.Should().StartWith(expectedTitlePrefix);
    }

    [Fact]
    public void ParseNaturalText_identifies_matching_account()
    {
        var result = IngestionService.ParseNaturalText("kopi 35k gopay", _accounts);
        result.AccountId.Should().Be(AccountGopay);
    }

    [Fact]
    public async Task IngestAsync_applies_matched_categorization_rule_and_persists()
    {
        var ruleRepo = new FakeCategorizationRuleRepository();
        var uow = new FakeLedgerUnitOfWork();
        var ruleService = new RuleService(ruleRepo, uow);

        // Add coffee rule
        var coffeeRule = CategorizationRule.Create("Coffee Rule", "kopi", targetCategoryId: CategoryCoffee, priority: 10);
        await ruleRepo.AddAsync(coffeeRule);

        var accountDir = new FakeAccountDirectory(AccountBca, AccountGopay);
        var categoryDir = new FakeCategoryDirectory();
        var txRepo = new FakeTransactionRepository();
        var ledgerService = new LedgerService(txRepo, accountDir, categoryDir, uow);

        var sut = new IngestionService(ledgerService, ruleService, accountDir, new SystemClock());

        var request = new IngestTransactionRequest(Text: "kopi 35k gopay");
        var result = await sut.IngestAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedRuleName.Should().Be("Coffee Rule");
        result.Value.Transaction.CategoryId.Should().Be(CategoryCoffee);
        result.Value.Transaction.Amount.Should().Be(35000m);
        txRepo.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task IngestAsync_replaces_title_when_configured_on_rule()
    {
        var ruleRepo = new FakeCategorizationRuleRepository();
        var uow = new FakeLedgerUnitOfWork();
        var ruleService = new RuleService(ruleRepo, uow);

        // Rule with ReplaceTitle
        var cleanRule = CategorizationRule.Create(
            "Indomaret Cleaner",
            "indomaret",
            targetCategoryId: CategoryCoffee,
            replaceTitle: "Indomaret Minimarket");
        await ruleRepo.AddAsync(cleanRule);

        var accountDir = new FakeAccountDirectory(AccountBca, AccountGopay);
        var categoryDir = new FakeCategoryDirectory();
        var txRepo = new FakeTransactionRepository();
        var ledgerService = new LedgerService(txRepo, accountDir, categoryDir, uow);

        var sut = new IngestionService(ledgerService, ruleService, accountDir, new SystemClock());

        var request = new IngestTransactionRequest(Text: "qris 99234 indomaret tgr 45k bca");
        var result = await sut.IngestAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedRuleName.Should().Be("Indomaret Cleaner");
        result.Value.Transaction.Title.Should().Be("Indomaret Minimarket");
    }

    [Fact]
    public async Task IngestAsync_smartly_infers_category_and_budget_envelope_when_no_explicit_rule_matches()
    {
        var ruleRepo = new FakeCategorizationRuleRepository();
        var uow = new FakeLedgerUnitOfWork();
        var ruleService = new RuleService(ruleRepo, uow);

        var budgetNeedsId = Guid.NewGuid();
        var categoryFoodId = Guid.NewGuid();

        var taxonomy = new List<CategoryTaxonomyRef>
        {
            new(budgetNeedsId, "Needs", "Budget", null, "Expense", true),
            new(categoryFoodId, "Food", "Category", budgetNeedsId, "Any", true),
        };

        var accountDir = new FakeAccountDirectory(AccountBca, AccountGopay);
        var categoryDir = new FakeCategoryDirectory("Any", true, taxonomy);
        var txRepo = new FakeTransactionRepository();
        var ledgerService = new LedgerService(txRepo, accountDir, categoryDir, uow);

        var sut = new IngestionService(ledgerService, ruleService, accountDir, new SystemClock(), categoryDir);

        var request = new IngestTransactionRequest(Text: "kopi kenangan 35k bca");
        var result = await sut.IngestAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Transaction.CategoryId.Should().Be(categoryFoodId);
        result.Value.Transaction.BudgetCategoryId.Should().Be(budgetNeedsId);
        result.Value.CategoryName.Should().Be("Food");
        result.Value.BudgetName.Should().Be("Needs");
        result.Value.AccountName.Should().Be("Test Account");
        result.Value.FormattedConfirmation.Should().Contain("Category: Food");
        result.Value.FormattedConfirmation.Should().Contain("Budget: Needs");
    }

    [Fact]
    public void ParseNaturalText_extracts_category_and_budget_in_full_scenario()
    {
        var result = IngestionService.ParseNaturalText("Add expense 45k lunch bca category Food budget Needs", _accounts);

        result.Amount.Should().Be(45000);
        result.Type.Should().Be("Expense");
        result.AccountId.Should().Be(AccountBca);
        result.CategoryName.Should().Be("Food");
        result.BudgetName.Should().Be("Needs");
        result.Title.Should().Be("Lunch");
    }

    [Fact]
    public void ParseNaturalText_extracts_category_and_budget_in_indonesian_full_scenario()
    {
        var result = IngestionService.ParseNaturalText("Catat makan siang 45k bca kategori Food budget Needs", _accounts);

        result.Amount.Should().Be(45000);
        result.Type.Should().Be("Expense");
        result.AccountId.Should().Be(AccountBca);
        result.CategoryName.Should().Be("Food");
        result.BudgetName.Should().Be("Needs");
        result.Title.Should().Be("Makan Siang");
    }

    [Fact]
    public async Task IngestAsync_resolves_explicit_category_and_budget_from_natural_scenario()
    {
        var ruleRepo = new FakeCategorizationRuleRepository();
        var uow = new FakeLedgerUnitOfWork();
        var ruleService = new RuleService(ruleRepo, uow);

        var budgetNeedsId = Guid.NewGuid();
        var categoryFoodId = Guid.NewGuid();

        var taxonomy = new List<CategoryTaxonomyRef>
        {
            new(budgetNeedsId, "Needs", "Budget", null, "Expense", true),
            new(categoryFoodId, "Food", "Category", budgetNeedsId, "Any", true),
        };

        var accountDir = new FakeAccountDirectory(_accounts);
        var categoryDir = new FakeCategoryDirectory("Any", true, taxonomy);
        var txRepo = new FakeTransactionRepository();
        var ledgerService = new LedgerService(txRepo, accountDir, categoryDir, uow);

        var sut = new IngestionService(ledgerService, ruleService, accountDir, new SystemClock(), categoryDir);

        var request = new IngestTransactionRequest(Text: "Add expense 45k lunch bca category Food budget Needs");
        var result = await sut.IngestAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Transaction.Title.Should().Be("Lunch");
        result.Value.Transaction.Amount.Should().Be(45000m);
        result.Value.Transaction.CategoryId.Should().Be(categoryFoodId);
        result.Value.Transaction.BudgetCategoryId.Should().Be(budgetNeedsId);
        result.Value.CategoryName.Should().Be("Food");
        result.Value.BudgetName.Should().Be("Needs");
        result.Value.AccountName.Should().Be("BCA");
        result.Value.FormattedConfirmation.Should().Contain("Category: Food");
        result.Value.FormattedConfirmation.Should().Contain("Budget: Needs");
    }
}
