using Microsoft.EntityFrameworkCore;
using Tameru.Accounts.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Accounts;

namespace Tameru.Accounts.Infrastructure;

/// <summary>
/// Implements the cross-module <see cref="IAccountDirectory"/> contract so other modules (Ledger)
/// can validate accounts without touching the Accounts tables directly (docs/ARCHITECTURE.md).
/// </summary>
internal sealed class AccountDirectory : IAccountDirectory
{
    private readonly AccountsDbContext _db;

    public AccountDirectory(AccountsDbContext db) => _db = db;

    public Task<bool> ExistsAndActiveAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.Accounts.AnyAsync(a => a.Id == accountId && a.IsActive, cancellationToken);

    public async Task<string?> GetCurrencyAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        await _db.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => a.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);
}
