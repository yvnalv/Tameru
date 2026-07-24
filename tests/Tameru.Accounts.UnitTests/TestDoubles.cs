using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Domain;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Accounts.UnitTests;

internal sealed class FakeAccountRepository : IAccountRepository
{
    public List<Account> Items { get; } = new();

    public FakeAccountRepository(params Account[] seed) => Items.AddRange(seed);

    public Task<IReadOnlyList<Account>> ListAsync(bool includeInactive, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Account>>(
            Items.Where(a => includeInactive || a.IsActive).ToList());

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        Items.Add(account);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAccountGroupRepository : IAccountGroupRepository
{
    public List<AccountGroup> Items { get; } = new();

    public FakeAccountGroupRepository(params AccountGroup[] seed) => Items.AddRange(seed);

    public Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AccountGroup>>(Items.ToList());

    public Task<AccountGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(g => g.Id == id));

    public Task<bool> AnyAsync(CancellationToken ct = default) => Task.FromResult(Items.Count > 0);

    public Task AddAsync(AccountGroup group, CancellationToken ct = default)
    {
        Items.Add(group);
        return Task.CompletedTask;
    }
}

/// <summary>Configurable stand-in for the Ledger contract (M3 provides the real one).</summary>
internal sealed class StubLedgerAccountQuery : ILedgerAccountQuery
{
    private readonly Dictionary<Guid, decimal> _movements = new();
    private readonly HashSet<Guid> _withTransactions = new();

    public StubLedgerAccountQuery WithMovement(Guid accountId, decimal amount)
    {
        _movements[accountId] = amount;
        return this;
    }

    public StubLedgerAccountQuery WithTransactions(Guid accountId)
    {
        _withTransactions.Add(accountId);
        return this;
    }

    public Task<IReadOnlyDictionary<Guid, decimal>> GetNetMovementByAccountAsync(
        DateOnly? asOf = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(_movements);

    public Task<decimal> GetNetMovementAsync(Guid accountId, DateOnly? asOf = null, CancellationToken ct = default) =>
        Task.FromResult(_movements.GetValueOrDefault(accountId));

    public Task<bool> HasTransactionsAsync(Guid accountId, CancellationToken ct = default) =>
        Task.FromResult(_withTransactions.Contains(accountId));
}

internal sealed class FakeAccountsUnitOfWork : IAccountsUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}
