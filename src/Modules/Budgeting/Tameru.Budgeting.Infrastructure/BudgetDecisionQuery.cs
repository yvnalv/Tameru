using Microsoft.EntityFrameworkCore;
using Tameru.Budgeting.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Budgeting.Infrastructure;

public sealed class BudgetDecisionQuery : IBudgetDecisionQuery
{
    private readonly BudgetingDbContext _db;
    private readonly ICategorySpendQuery _spend;

    public BudgetDecisionQuery(BudgetingDbContext db, ICategorySpendQuery spend)
    {
        _db = db;
        _spend = spend;
    }

    public async Task<IReadOnlyList<CategoryBudgetStatus>> GetCategoryBudgetsAsync(
        int year, int month, int startDay = 1, CancellationToken ct = default)
    {
        var period = await _db.BudgetPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

        var day = Math.Clamp(startDay, 1, 28);
        DateOnly first;
        DateOnly last;
        if (day == 1)
        {
            first = new DateOnly(year, month, 1);
            last = first.AddMonths(1).AddDays(-1);
        }
        else
        {
            var clampedDay = Math.Min(day, DateTime.DaysInMonth(year, month));
            first = new DateOnly(year, month, clampedDay);
            var nextMonth = first.AddMonths(1);
            last = nextMonth.AddDays(-1);
        }

        var actuals = await _spend.GetExpenseTotalsByCategoryAsync(first, last, ct);
        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        Dictionary<Guid, decimal> lineDict = [];
        if (period is not null)
        {
            var lines = await _db.BudgetLines.AsNoTracking()
                .Where(l => l.BudgetPeriodId == period.Id)
                .ToListAsync(ct);
            lineDict = lines.ToDictionary(l => l.CategoryId, l => l.PlanAmount);
        }

        return categories
            .Select(c =>
            {
                var plan = lineDict.GetValueOrDefault(c.Id, 0m);
                var act = actuals.GetValueOrDefault(c.Id, 0m);
                return new CategoryBudgetStatus(c.Id, c.Name, plan, act, plan - act);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<FixedObligationRef>> GetFixedObligationsAsync(CancellationToken ct = default)
    {
        var needsSections = await _db.MasterPlanSections.AsNoTracking()
            .Where(s => s.Name.ToLower().Contains("need") || s.Name.ToLower().Contains("kebutuhan") || s.Name.ToLower().Contains("wajib"))
            .ToListAsync(ct);

        if (needsSections.Count == 0)
        {
            needsSections = await _db.MasterPlanSections.AsNoTracking().ToListAsync(ct);
        }

        var sectionIds = needsSections.Select(s => s.Id).ToList();
        var items = await _db.MasterPlanItems.AsNoTracking()
            .Where(i => sectionIds.Contains(i.SectionId))
            .OrderBy(i => i.SortOrder)
            .ToListAsync(ct);

        var sectionNames = needsSections.ToDictionary(s => s.Id, s => s.Name);
        return items.Select(i => new FixedObligationRef(
            i.Id,
            i.Name,
            i.TotalBudget,
            sectionNames.GetValueOrDefault(i.SectionId, "Needs")
        )).ToList();
    }
}
