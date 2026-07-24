using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Domain;
using Tameru.Ledger.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Ledger.Infrastructure;

/// <summary>
/// The real <see cref="ILedgerAccountQuery"/> — derives account movement from the ledger in SQL,
/// mirroring <c>BalanceCalculator</c> (ADR-0006, docs/DATABASE.md → Derived balance). Voided
/// (soft-deleted) transactions are excluded by the context's global query filter. Replaces the
/// Accounts module's no-op default when the Ledger module is registered.
/// </summary>
internal sealed class LedgerAccountQuery : ILedgerAccountQuery
{
    private readonly LedgerDbContext _db;

    public LedgerAccountQuery(LedgerDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetNetMovementByAccountAsync(
        DateOnly? asOf = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Transactions.AsQueryable();
        if (asOf is { } cutoff)
        {
            query = query.Where(t => t.Date <= cutoff);
        }

        var income = await query
            .Where(t => t.Type == TransactionType.Income)
            .GroupBy(t => t.AccountId)
            .Select(g => new { Account = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var outflow = await query
            .Where(t => t.Type != TransactionType.Income)
            .GroupBy(t => t.AccountId)
            .Select(g => new { Account = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var transferIn = await query
            .Where(t => t.Type == TransactionType.Transfer && t.ToAccountId != null)
            .GroupBy(t => t.ToAccountId!.Value)
            .Select(g => new { Account = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, decimal>();
        foreach (var row in income)
        {
            result[row.Account] = result.GetValueOrDefault(row.Account) + row.Sum;
        }

        foreach (var row in outflow)
        {
            result[row.Account] = result.GetValueOrDefault(row.Account) - row.Sum;
        }

        foreach (var row in transferIn)
        {
            result[row.Account] = result.GetValueOrDefault(row.Account) + row.Sum;
        }

        return result;
    }

    public async Task<decimal> GetNetMovementAsync(
        Guid accountId, DateOnly? asOf = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Transactions.AsQueryable();
        if (asOf is { } cutoff)
        {
            query = query.Where(t => t.Date <= cutoff);
        }

        var income = await query
            .Where(t => t.AccountId == accountId && t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var outflow = await query
            .Where(t => t.AccountId == accountId && t.Type != TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var transferIn = await query
            .Where(t => t.Type == TransactionType.Transfer && t.ToAccountId == accountId)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return income - outflow + transferIn;
    }

    public Task<bool> HasTransactionsAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _db.Transactions.AnyAsync(t => t.AccountId == accountId || t.ToAccountId == accountId, cancellationToken);
}
