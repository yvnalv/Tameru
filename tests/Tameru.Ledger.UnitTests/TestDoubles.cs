using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.UnitTests;

/// <summary>Category directory stub. Accepts all categories with a configurable flow (default Any).</summary>
internal sealed class FakeCategoryDirectory : ICategoryDirectory
{
    private readonly string _flow;
    private readonly bool _active;

    public List<CategoryTaxonomyRef> Taxonomy { get; } = new();

    public FakeCategoryDirectory(string flow = "Any", bool active = true, IEnumerable<CategoryTaxonomyRef>? taxonomy = null)
    {
        _flow = flow;
        _active = active;
        if (taxonomy is not null)
        {
            Taxonomy.AddRange(taxonomy);
        }
    }

    public Task<CategoryRef?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CategoryRef?>(new CategoryRef(categoryId, "Category", _flow, _active));

    public Task<IReadOnlyList<CategoryTaxonomyRef>> ListActiveTaxonomyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CategoryTaxonomyRef>>(Taxonomy);
}

internal sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Transaction> Items { get; } = new();

    public Task<PagedResult<Transaction>> ListAsync(TransactionFilter filter, CancellationToken ct = default)
    {
        var items = Items.OrderByDescending(t => t.Date).ToList();
        return Task.FromResult(new PagedResult<Transaction>(items, filter.Page, filter.PageSize, items.Count));
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        Items.Add(transaction);
        return Task.CompletedTask;
    }

    public void Remove(Transaction transaction) => Items.Remove(transaction);
}

internal sealed class FakeAccountDirectory : IAccountDirectory
{
    private readonly HashSet<Guid> _active = new();

    public FakeAccountDirectory(params Guid[] activeAccounts) => _active.UnionWith(activeAccounts);

    public Task<bool> ExistsAndActiveAsync(Guid accountId, CancellationToken ct = default) =>
        Task.FromResult(_active.Contains(accountId));

    public Task<string?> GetCurrencyAsync(Guid accountId, CancellationToken ct = default) =>
        Task.FromResult<string?>(_active.Contains(accountId) ? "IDR" : null);

    public Task<IReadOnlyList<AccountRef>> ListActiveAccountsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AccountRef>>(_active.Select(id => new AccountRef(id, "Test Account", "Bank", "IDR")).ToList());
}

internal sealed class FakeLedgerUnitOfWork : ILedgerUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeCategorizationRuleRepository : ICategorizationRuleRepository
{
    public List<CategorizationRule> Items { get; } = new();

    public Task<IReadOnlyList<CategorizationRule>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var list = activeOnly ? Items.Where(r => r.IsActive).OrderByDescending(r => r.Priority).ToList() : Items.ToList();
        return Task.FromResult<IReadOnlyList<CategorizationRule>>(list);
    }

    public Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(r => r.Id == id));

    public Task AddAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        Items.Add(rule);
        return Task.CompletedTask;
    }

    public void Remove(CategorizationRule rule) => Items.Remove(rule);
}
