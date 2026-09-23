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

    public Task<IReadOnlyList<CategoryPeriodTotal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default) =>
        GetCategoryTotalsAsync(from, to, ReportFlow.Expense, granularity, cancellationToken);

    public async Task<IReadOnlyList<CategoryPeriodTotal>> GetCategoryTotalsAsync(
        DateOnly from, DateOnly to, ReportFlow flow, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        var targetType = flow == ReportFlow.Income ? TransactionType.Income : TransactionType.Expense;
        var txns = _db.Transactions
            .Where(t => t.Type == targetType
                && t.CategoryId != null
                && t.Date >= from && t.Date <= to);

        if (granularity == ReportGranularity.Monthly)
        {
            var rows = await txns
                .GroupBy(t => new { t.CategoryId, t.Date.Year, t.Date.Month })
                .Select(g => new { g.Key.CategoryId, g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new CategoryPeriodTotal(
                    r.CategoryId!.Value, new DateOnly(r.Year, r.Month, 1), r.Sum))
                .ToList();
        }

        var daily = await txns
            .GroupBy(t => new { t.CategoryId, t.Date })
            .Select(g => new { g.Key.CategoryId, g.Key.Date, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return daily
            .Select(r => new CategoryPeriodTotal(r.CategoryId!.Value, r.Date, r.Sum))
            .ToList();
    }

    public async Task<IReadOnlyList<EnvelopePeriodTotal>> GetEnvelopeTotalsAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        var expenses = _db.Transactions
            .Where(t => t.Type == TransactionType.Expense
                && t.Date >= from && t.Date <= to);

        if (granularity == ReportGranularity.Monthly)
        {
            var rows = await expenses
                .GroupBy(t => new { t.BudgetCategoryId, t.Date.Year, t.Date.Month })
                .Select(g => new { g.Key.BudgetCategoryId, g.Key.Year, g.Key.Month, Sum = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);

            return rows
                .Select(r => new EnvelopePeriodTotal(
                    r.BudgetCategoryId, new DateOnly(r.Year, r.Month, 1), r.Sum))
                .ToList();
        }

        var daily = await expenses
            .GroupBy(t => new { t.BudgetCategoryId, t.Date })
            .Select(g => new { g.Key.BudgetCategoryId, g.Key.Date, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return daily
            .Select(r => new EnvelopePeriodTotal(r.BudgetCategoryId, r.Date, r.Sum))
            .ToList();
    }

    public async Task<IReadOnlyList<DayOfWeekSpend>> GetDayOfWeekSpendAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var dailyExpenses = await _db.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= from && t.Date <= to)
            .GroupBy(t => t.Date)
            .Select(g => new { Date = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var byDow = dailyExpenses
            .GroupBy(d => d.Date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Sum));

        var totalDaysInPeriod = new Dictionary<DayOfWeek, int>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var dow = d.DayOfWeek;
            totalDaysInPeriod[dow] = totalDaysInPeriod.GetValueOrDefault(dow) + 1;
        }

        var result = new List<DayOfWeekSpend>();
        foreach (DayOfWeek dow in Enum.GetValues<DayOfWeek>())
        {
            var total = byDow.TryGetValue(dow, out var s) ? s : 0m;
            var days = totalDaysInPeriod.GetValueOrDefault(dow, 1);
            var avg = days > 0 ? Math.Round(total / days, 2) : 0m;
            result.Add(new DayOfWeekSpend(dow, total, days, avg));
        }

        return result;
    }

    public async Task<IReadOnlyList<PayeeSpendTotal>> GetTopPayeesAsync(
        DateOnly from, DateOnly to, int limit = 5, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Max(1, Math.Min(limit, 20));
        var top = await _db.Transactions
            .Where(t => t.Type == TransactionType.Expense && t.Date >= from && t.Date <= to && !string.IsNullOrWhiteSpace(t.Title))
            .GroupBy(t => t.Title.Trim())
            .Select(g => new { Payee = g.Key, Total = g.Sum(x => x.Amount), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return top
            .Select(x => new PayeeSpendTotal(x.Payee, x.Total, x.Count))
            .ToList();
    }
}
