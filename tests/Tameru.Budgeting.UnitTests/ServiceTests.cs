using FluentAssertions;
using Tameru.Budgeting.Application;
using Tameru.Budgeting.Application.Contracts;
using Tameru.Budgeting.Domain;
using Xunit;

namespace Tameru.Budgeting.UnitTests;

public class CategoryServiceTests
{
    private readonly FakeCategoryRepository _repo = new();
    private readonly FakeBudgetingUnitOfWork _uow = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests() => _sut = new CategoryService(_repo, _uow);

    [Fact]
    public async Task Create_category_requires_parent_of_the_level_above()
    {
        var budget = Category.Create("Needs", CategoryLevel.Budget, null, CategoryFlow.Expense);
        _repo.Items.Add(budget);

        var ok = await _sut.CreateAsync(new CreateCategoryRequest("Food", "Category", budget.Id, "Any", 0));
        ok.IsSuccess.Should().BeTrue();

        var badParent = await _sut.CreateAsync(new CreateCategoryRequest("Sub", "Sub", budget.Id, "Any", 0));
        badParent.Error.Code.Should().Be("category_invalid_parent");
    }

    [Fact]
    public async Task Create_with_unknown_parent_fails()
    {
        var result = await _sut.CreateAsync(new CreateCategoryRequest("Food", "Category", Guid.NewGuid(), null, 0));

        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Deactivate_blocked_when_category_has_children()
    {
        var budget = Category.Create("Needs", CategoryLevel.Budget, null, CategoryFlow.Expense);
        var child = Category.Create("Food", CategoryLevel.Category, budget.Id);
        _repo.Items.Add(budget);
        _repo.Items.Add(child);

        var result = await _sut.DeactivateAsync(budget.Id);

        result.Error.Code.Should().Be("category_in_use");
    }
}

public class BudgetServiceTests
{
    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly StubCategorySpendQuery _spend = new();
    private readonly FakeBudgetingUnitOfWork _uow = new();
    private readonly BudgetService _sut;

    public BudgetServiceTests() =>
        _sut = new BudgetService(_budgets, _categories, _spend, _uow);

    [Fact]
    public async Task Create_period_is_unique_per_year_month()
    {
        (await _sut.CreatePeriodAsync(new CreateBudgetPeriodRequest(2026, 6, null))).IsSuccess.Should().BeTrue();

        var dup = await _sut.CreatePeriodAsync(new CreateBudgetPeriodRequest(2026, 6, null));
        dup.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Actual_and_leftover_derive_from_ledger_spend()
    {
        var food = Category.Create("Food", CategoryLevel.Category,
            Category.Create("Needs", CategoryLevel.Budget, null).Id);
        _categories.Items.Add(food);
        var created = await _sut.CreatePeriodAsync(new CreateBudgetPeriodRequest(2026, 6, null));
        var periodId = created.Value.Id;

        await _sut.UpsertLinesAsync(periodId,
            new UpsertBudgetLinesRequest([new BudgetLineInput(food.Id, 770_000m)]));
        _spend.With(food.Id, 500_000m);

        var period = (await _sut.GetPeriodAsync(2026, 6)).Value;

        period.Lines.Should().ContainSingle();
        var line = period.Lines[0];
        line.Plan.Should().Be(770_000m);
        line.Actual.Should().Be(500_000m);
        line.Leftover.Should().Be(270_000m);
        period.TotalLeftover.Should().Be(270_000m);
    }

    [Fact]
    public async Task Get_missing_period_returns_not_found()
    {
        var result = await _sut.GetPeriodAsync(2099, 1);

        result.Error.Code.Should().Be("not_found");
    }
}

public class MasterPlanServiceTests
{
    private readonly FakeMasterPlanRepository _repo = new();
    private readonly FakeBudgetingUnitOfWork _uow = new();
    private readonly MasterPlanService _sut;

    public MasterPlanServiceTests() => _sut = new MasterPlanService(_repo, _uow);

    [Fact]
    public async Task Get_rolls_up_item_totals_per_section()
    {
        var section = MasterPlanSection.Create("Needs", 50m);
        _repo.Sections.Add(section);
        await _sut.CreateItemAsync(new CreateMasterPlanItemRequest(section.Id, "Lunch", 25_000m, 22, 0));
        await _sut.CreateItemAsync(new CreateMasterPlanItemRequest(section.Id, "Breakfast", 10_000m, 22, 1));

        var plan = await _sut.GetAsync();

        plan.Sections.Should().ContainSingle();
        plan.Sections[0].Total.Should().Be(770_000m);
        plan.GrandTotal.Should().Be(770_000m);
    }

    [Fact]
    public async Task Create_item_in_unknown_section_fails()
    {
        var result = await _sut.CreateItemAsync(new CreateMasterPlanItemRequest(Guid.NewGuid(), "X", 1m, 1, 0));

        result.Error.Code.Should().Be("not_found");
    }
}
