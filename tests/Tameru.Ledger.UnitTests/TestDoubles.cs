using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.UnitTests;

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
