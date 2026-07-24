using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Domain;
using Tameru.Ledger.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Ledger.Infrastructure;

/// <summary>
/// The real <see cref="ILedgerReportingQuery"/> — aggregate reads over the ledger for the Reporting
/// module (cashflow trend, category pivots). Voided (soft-deleted) transactions are excluded by the
/// context's global query filter (BR-007). The ledger stays the single source of truth (ADR-0006).
/// </summary>
internal sealed class LedgerReportingQuery : ILedgerReportingQuery
{
    private readonly LedgerDbContext _db;

    public LedgerReportingQuery(LedgerDbContext db) => _db = db;

    public async Task<IReadOnlyList<MonthlyCashflow>> GetMonthlyCashflowAsync(
        int year, CancellationToken cancellationToken = default)
    {
        var first = new DateOnly(year, 1, 1);
        var last = new DateOnly(year, 12, 31);

        var byMonth = await _db.Transactions
            .Where(t => t.Date >= first && t.Date <= last
                && (t.Type == TransactionType.Income || t.Type == TransactionType.Expense))
            .GroupBy(t => new { t.Date.Month, t.Type })
            .Select(g => new { g.Key.Month, g.Key.Type, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return Enumerable.Range(1, 12)
            .Select(month => new MonthlyCashflow(
                month,
                Income: byMonth.Where(r => r.Month == month && r.Type == TransactionType.Income)
                    .Sum(r => r.Sum),
                Expense: byMonth.Where(r => r.Month == month && r.Type == TransactionType.Expense)
                    .Sum(r => r.Sum)))
            .ToList();
    }

    public async Task<IReadOnlyList<CategoryPeriodTotal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        var expenses = _db.Transactions
            .Where(t => t.Type == TransactionType.Expense
                && t.CategoryId != null
                && t.Date >= from && t.Date <= to);

        if (granularity == ReportGranularity.Monthly)
        {
            var rows = await expenses
                .GroupBy(t => new { t.CategoryId, t.Date.Year, t.Date.Month })
                .Select(g => new { g.Key.CategoryId, g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new CategoryPeriodTotal(
                    r.CategoryId!.Value, new DateOnly(r.Year, r.Month, 1), r.Sum))
                .ToList();
        }

        var daily = await expenses
            .GroupBy(t => new { t.CategoryId, t.Date })
            .Select(g => new { g.Key.CategoryId, g.Key.Date, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return daily
            .Select(r => new CategoryPeriodTotal(r.CategoryId!.Value, r.Date, r.Sum))
            .ToList();
    }
}
