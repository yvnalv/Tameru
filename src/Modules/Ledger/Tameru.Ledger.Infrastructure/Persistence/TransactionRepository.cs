using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Infrastructure.Persistence;

internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly LedgerDbContext _db;

    public TransactionRepository(LedgerDbContext db) => _db = db;

    public async Task<PagedResult<Transaction>> ListAsync(TransactionFilter filter, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Type)
            && Enum.TryParse<TransactionType>(filter.Type, ignoreCase: true, out var type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<TransactionStatus>(filter.Status, ignoreCase: true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (filter.AccountId is { } accountId)
        {
            query = query.Where(t => t.AccountId == accountId || t.ToAccountId == accountId);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(t =>
                t.CategoryId == categoryId || t.BudgetCategoryId == categoryId || t.SubCategoryId == categoryId);
        }

        if (filter.From is { } from)
        {
            query = query.Where(t => t.Date >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(t => t.Date <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var q = filter.Query.Trim();
            query = query.Where(t => EF.Functions.ILike(t.Title, $"%{q}%"));
        }

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Transaction>(items, page, pageSize, total);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default) =>
        await _db.Transactions.AddAsync(transaction, ct);

    public void Remove(Transaction transaction) => _db.Transactions.Remove(transaction);
}
