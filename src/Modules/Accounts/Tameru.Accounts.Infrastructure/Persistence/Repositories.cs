using Microsoft.EntityFrameworkCore;
using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Domain;

namespace Tameru.Accounts.Infrastructure.Persistence;

internal sealed class AccountRepository : IAccountRepository
{
    private readonly AccountsDbContext _db;

    public AccountRepository(AccountsDbContext db) => _db = db;

    public async Task<IReadOnlyList<Account>> ListAsync(bool includeInactive, CancellationToken ct = default) =>
        await _db.Accounts
            .Where(a => includeInactive || a.IsActive)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(ct);

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Account account, CancellationToken ct = default) =>
        await _db.Accounts.AddAsync(account, ct);
}

internal sealed class AccountGroupRepository : IAccountGroupRepository
{
    private readonly AccountsDbContext _db;

    public AccountGroupRepository(AccountsDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct = default) =>
        await _db.AccountGroups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync(ct);

    public Task<AccountGroup?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.AccountGroups.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.AccountGroups.AnyAsync(ct);

    public async Task AddAsync(AccountGroup group, CancellationToken ct = default) =>
        await _db.AccountGroups.AddAsync(group, ct);
}
