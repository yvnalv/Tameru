using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Domain;
using Tameru.Ledger.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Ledger.Infrastructure;

/// <summary>
/// The real <see cref="ICategorySpendQuery"/> — sums non-voided expense amounts for a month grouped
/// by each classifying category id (budget / category / sub level). Consumed by Budgeting to compute
/// a budget's Actual (BR-062). Voided transactions are excluded by the global query filter.
/// </summary>
internal sealed class CategorySpendQuery : ICategorySpendQuery
{
    private readonly LedgerDbContext _db;

    public CategorySpendQuery(LedgerDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetExpenseTotalsByCategoryAsync(
        int year, int month, CancellationToken cancellationToken = default)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var expenses = _db.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= first && t.Date <= last);

        var totals = new Dictionary<Guid, decimal>();

        await AccumulateAsync(expenses, t => t.BudgetCategoryId, totals, cancellationToken);
        await AccumulateAsync(expenses, t => t.CategoryId, totals, cancellationToken);
        await AccumulateAsync(expenses, t => t.SubCategoryId, totals, cancellationToken);

        return totals;
    }

    private static async Task AccumulateAsync(
        IQueryable<Transaction> expenses,
        System.Linq.Expressions.Expression<Func<Transaction, Guid?>> categorySelector,
        Dictionary<Guid, decimal> totals,
        CancellationToken ct)
    {
        var rows = await expenses
            .Where(BuildNotNull(categorySelector))
            .GroupBy(categorySelector)
            .Select(g => new { Category = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            if (row.Category is { } id)
            {
                totals[id] = totals.GetValueOrDefault(id) + row.Sum;
            }
        }
    }

    private static System.Linq.Expressions.Expression<Func<Transaction, bool>> BuildNotNull(
        System.Linq.Expressions.Expression<Func<Transaction, Guid?>> selector)
    {
        var parameter = selector.Parameters[0];
        var notNull = System.Linq.Expressions.Expression.NotEqual(
            selector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(Guid?)));
        return System.Linq.Expressions.Expression.Lambda<Func<Transaction, bool>>(notNull, parameter);
    }
}
