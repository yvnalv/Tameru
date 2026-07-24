using FluentAssertions;
using Tameru.Budgeting.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Budgeting.UnitTests;

public class CategoryTests
{
    [Fact]
    public void Budget_level_must_not_have_a_parent()
    {
        var act = () => Category.Create("Needs", CategoryLevel.Budget, Guid.NewGuid());

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("category_budget_no_parent");
    }

    [Fact]
    public void Category_level_requires_a_parent()
    {
        var act = () => Category.Create("Food", CategoryLevel.Category, null);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("category_parent_required");
    }

    [Fact]
    public void System_category_cannot_be_deactivated()
    {
        var income = Category.Create("Income", CategoryLevel.Budget, null, CategoryFlow.Income, isSystem: true);

        var act = () => income.Deactivate();

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("category_is_system");
    }

    [Theory]
    [InlineData(CategoryFlow.Any, CategoryFlow.Expense, true)]
    [InlineData(CategoryFlow.Expense, CategoryFlow.Expense, true)]
    [InlineData(CategoryFlow.Income, CategoryFlow.Expense, false)]
    public void AcceptsFlow_matches_any_or_exact(CategoryFlow categoryFlow, CategoryFlow txnFlow, bool expected)
    {
        var budget = Category.Create("X", CategoryLevel.Budget, null, categoryFlow);

        budget.AcceptsFlow(txnFlow).Should().Be(expected);
    }
}

public class MasterPlanTests
{
    [Fact]
    public void Item_total_is_price_times_frequency()
    {
        var item = MasterPlanItem.Create(Guid.NewGuid(), "Breakfast", 10_000m, 22);

        item.TotalBudget.Should().Be(220_000m);
    }

    [Fact]
    public void Section_target_is_clamped_to_0_100()
    {
        var section = MasterPlanSection.Create("Needs", 150m);

        section.TargetPercent.Should().Be(100m);
    }
}

public class BudgetDomainTests
{
    [Fact]
    public void Period_rejects_invalid_month()
    {
        var act = () => BudgetPeriod.Create(2026, 13);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("budget_month_invalid");
    }

    [Fact]
    public void Line_rejects_negative_plan()
    {
        var act = () => BudgetLine.Create(Guid.NewGuid(), Guid.NewGuid(), -1m);

        act.Should().Throw<DomainRuleException>().Which.Code.Should().Be("budget_plan_negative");
    }
}
