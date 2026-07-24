using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Domain;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Budgeting.UnitTests;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Items { get; } = new();

    public FakeCategoryRepository(params Category[] seed) => Items.AddRange(seed);

    public Task<IReadOnlyList<Category>> ListAsync(
        CategoryLevel? level, CategoryFlow? flow, Guid? parentId, bool includeInactive, CancellationToken ct = default)
    {
        IEnumerable<Category> q = Items;
        if (level is { } l) q = q.Where(c => c.Level == l);
        if (flow is { } f) q = q.Where(c => c.Flow == f);
        if (parentId is { } p) q = q.Where(c => c.ParentId == p);
        if (!includeInactive) q = q.Where(c => c.IsActive);
        return Task.FromResult<IReadOnlyList<Category>>(q.ToList());
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id));

    public Task<bool> AnyAsync(CancellationToken ct = default) => Task.FromResult(Items.Count > 0);

    public Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(c => c.ParentId == categoryId));

    public Task AddAsync(Category category, CancellationToken ct = default)
    {
        Items.Add(category);
        return Task.CompletedTask;
    }
}

internal sealed class FakeBudgetRepository : IBudgetRepository
{
    public List<BudgetPeriod> Periods { get; } = new();
    public List<BudgetLine> Lines { get; } = new();

    public Task<BudgetPeriod?> GetPeriodAsync(int year, int month, CancellationToken ct = default) =>
        Task.FromResult(Periods.FirstOrDefault(p => p.Year == year && p.Month == month));

    public Task<BudgetPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Periods.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<BudgetPeriod>> ListPeriodsAsync(int? year, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BudgetPeriod>>(
            Periods.Where(p => year is null || p.Year == year).ToList());

    public Task<IReadOnlyList<BudgetLine>> ListLinesAsync(Guid periodId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BudgetLine>>(Lines.Where(l => l.BudgetPeriodId == periodId).ToList());

    public Task AddPeriodAsync(BudgetPeriod period, CancellationToken ct = default)
    {
        Periods.Add(period);
        return Task.CompletedTask;
    }

    public Task AddLineAsync(BudgetLine line, CancellationToken ct = default)
    {
        Lines.Add(line);
        return Task.CompletedTask;
    }
}

internal sealed class FakeMasterPlanRepository : IMasterPlanRepository
{
    public List<MasterPlanSection> Sections { get; } = new();
    public List<MasterPlanItem> Items { get; } = new();

    public Task<IReadOnlyList<MasterPlanSection>> ListSectionsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MasterPlanSection>>(Sections.ToList());

    public Task<MasterPlanSection?> GetSectionAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Sections.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<MasterPlanItem>> ListItemsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MasterPlanItem>>(Items.ToList());

    public Task<MasterPlanItem?> GetItemAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<bool> AnySectionsAsync(CancellationToken ct = default) => Task.FromResult(Sections.Count > 0);

    public Task AddSectionAsync(MasterPlanSection section, CancellationToken ct = default)
    {
        Sections.Add(section);
        return Task.CompletedTask;
    }

    public Task AddItemAsync(MasterPlanItem item, CancellationToken ct = default)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public void RemoveItem(MasterPlanItem item) => Items.Remove(item);
}

/// <summary>Stub of the Ledger spend contract with configurable per-category actuals.</summary>
internal sealed class StubCategorySpendQuery : ICategorySpendQuery
{
    private readonly Dictionary<Guid, decimal> _totals = new();

    public StubCategorySpendQuery With(Guid categoryId, decimal amount)
    {
        _totals[categoryId] = amount;
        return this;
    }

    public Task<IReadOnlyDictionary<Guid, decimal>> GetExpenseTotalsByCategoryAsync(
        int year, int month, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(_totals);
}

internal sealed class FakeBudgetingUnitOfWork : IBudgetingUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}
