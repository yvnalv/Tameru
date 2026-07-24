using Tameru.SharedKernel.Domain;

namespace Tameru.Budgeting.Domain;

/// <summary>
/// A month for which budget lines are planned (unique per year+month, BR-060). Actual and Leftover
/// are derived from the ledger (BR-062) and are never stored here.
/// </summary>
public sealed class BudgetPeriod : AuditableEntity
{
    private BudgetPeriod()
    {
    }

    private BudgetPeriod(Guid id, int year, int month, string? note) : base(id)
    {
        Year = year;
        Month = month;
        Note = note;
    }

    public int Year { get; private set; }

    public int Month { get; private set; }

    public string? Note { get; private set; }

    public static BudgetPeriod Create(int year, int month, string? note = null)
    {
        if (year is < 2000 or > 2100)
        {
            throw new DomainRuleException("budget_year_invalid", "Year is out of range.");
        }

        if (month is < 1 or > 12)
        {
            throw new DomainRuleException("budget_month_invalid", "Month must be between 1 and 12.");
        }

        return new BudgetPeriod(Guid.NewGuid(), year, month, note?.Trim());
    }

    public void SetNote(string? note) => Note = note?.Trim();
}

/// <summary>A planned amount for one category within a budget period (unique per period+category, BR-061).</summary>
public sealed class BudgetLine : AuditableEntity
{
    private BudgetLine()
    {
    }

    private BudgetLine(Guid id, Guid budgetPeriodId, Guid categoryId, decimal planAmount) : base(id)
    {
        BudgetPeriodId = budgetPeriodId;
        CategoryId = categoryId;
        PlanAmount = planAmount;
    }

    public Guid BudgetPeriodId { get; private set; }

    public Guid CategoryId { get; private set; }

    public decimal PlanAmount { get; private set; }

    public static BudgetLine Create(Guid budgetPeriodId, Guid categoryId, decimal planAmount)
    {
        if (planAmount < 0m)
        {
            throw new DomainRuleException("budget_plan_negative", "A planned amount cannot be negative.");
        }

        return new BudgetLine(Guid.NewGuid(), budgetPeriodId, categoryId, planAmount);
    }

    public void SetPlan(decimal planAmount)
    {
        if (planAmount < 0m)
        {
            throw new DomainRuleException("budget_plan_negative", "A planned amount cannot be negative.");
        }

        PlanAmount = planAmount;
    }
}
