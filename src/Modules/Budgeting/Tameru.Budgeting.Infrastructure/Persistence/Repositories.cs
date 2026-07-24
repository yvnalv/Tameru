using Microsoft.EntityFrameworkCore;
using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Domain;

namespace Tameru.Budgeting.Infrastructure.Persistence;

internal sealed class CategoryRepository : ICategoryRepository
{
    private readonly BudgetingDbContext _db;

    public CategoryRepository(BudgetingDbContext db) => _db = db;

    public async Task<IReadOnlyList<Category>> ListAsync(
        CategoryLevel? level, CategoryFlow? flow, Guid? parentId, bool includeInactive, CancellationToken ct = default)
    {
        var query = _db.Categories.AsQueryable();
        if (level is { } l)
        {
            query = query.Where(c => c.Level == l);
        }

        if (flow is { } f)
        {
            query = query.Where(c => c.Flow == f);
        }

        if (parentId is { } p)
        {
            query = query.Where(c => c.ParentId == p);
        }

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.Level).ThenBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Categories.AnyAsync(ct);

    public Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken ct = default) =>
        _db.Categories.AnyAsync(c => c.ParentId == categoryId, ct);

    public async Task AddAsync(Category category, CancellationToken ct = default) =>
        await _db.Categories.AddAsync(category, ct);
}

internal sealed class BudgetRepository : IBudgetRepository
{
    private readonly BudgetingDbContext _db;

    public BudgetRepository(BudgetingDbContext db) => _db = db;

    public Task<BudgetPeriod?> GetPeriodAsync(int year, int month, CancellationToken ct = default) =>
        _db.BudgetPeriods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, ct);

    public Task<BudgetPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.BudgetPeriods.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<BudgetPeriod>> ListPeriodsAsync(int? year, CancellationToken ct = default)
    {
        var query = _db.BudgetPeriods.AsQueryable();
        if (year is { } y)
        {
            query = query.Where(p => p.Year == y);
        }

        return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<BudgetLine>> ListLinesAsync(Guid periodId, CancellationToken ct = default) =>
        await _db.BudgetLines.Where(l => l.BudgetPeriodId == periodId).ToListAsync(ct);

    public async Task AddPeriodAsync(BudgetPeriod period, CancellationToken ct = default) =>
        await _db.BudgetPeriods.AddAsync(period, ct);

    public async Task AddLineAsync(BudgetLine line, CancellationToken ct = default) =>
        await _db.BudgetLines.AddAsync(line, ct);
}

internal sealed class MasterPlanRepository : IMasterPlanRepository
{
    private readonly BudgetingDbContext _db;

    public MasterPlanRepository(BudgetingDbContext db) => _db = db;

    public async Task<IReadOnlyList<MasterPlanSection>> ListSectionsAsync(CancellationToken ct = default) =>
        await _db.MasterPlanSections.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync(ct);

    public Task<MasterPlanSection?> GetSectionAsync(Guid id, CancellationToken ct = default) =>
        _db.MasterPlanSections.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<MasterPlanItem>> ListItemsAsync(CancellationToken ct = default) =>
        await _db.MasterPlanItems.ToListAsync(ct);

    public Task<MasterPlanItem?> GetItemAsync(Guid id, CancellationToken ct = default) =>
        _db.MasterPlanItems.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> AnySectionsAsync(CancellationToken ct = default) => _db.MasterPlanSections.AnyAsync(ct);

    public async Task AddSectionAsync(MasterPlanSection section, CancellationToken ct = default) =>
        await _db.MasterPlanSections.AddAsync(section, ct);

    public async Task AddItemAsync(MasterPlanItem item, CancellationToken ct = default) =>
        await _db.MasterPlanItems.AddAsync(item, ct);

    public void RemoveItem(MasterPlanItem item) => _db.MasterPlanItems.Remove(item);
}
