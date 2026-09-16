using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Results;

namespace Tameru.Reporting.Application;

/// <summary>
/// Read-only analytics for the dashboard. Reporting owns no data: every figure is computed on read
/// from the Accounts and Ledger modules through their contracts (docs/MODULES.md → Reporting), so
/// reports are always consistent with the ledger, the single source of truth (ADR-0006).
/// </summary>
public sealed class ReportingService
{
    /// <summary>Functional currency (IDR). Multi-currency reporting is a reserved future capability.</summary>
    private const string FunctionalCurrency = "IDR";

    private readonly IAccountBalanceDirectory _accounts;
    private readonly ILedgerReportingQuery _ledger;

    public ReportingService(IAccountBalanceDirectory accounts, ILedgerReportingQuery ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    /// <summary>Net worth over active accounts (BR-023) plus the per-account breakdown.</summary>
    public async Task<Result<NetWorthReport>> GetNetWorthAsync(CancellationToken ct = default)
    {
        var balances = await _accounts.GetBalancesAsync(activeOnly: true, ct);
        var accounts = balances
            .Select(b => new AccountBalanceDto(b.Id, b.Name, b.GroupName, b.Type, b.Balance, b.CurrencyCode))
            .ToList();

        return new NetWorthReport(accounts.Sum(a => a.Balance), FunctionalCurrency, accounts);
    }

    /// <summary>Income vs. expense for the given month, with the full-year 12-month trend.</summary>
    public async Task<Result<CashflowReport>> GetCashflowAsync(
        int year, int month, CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
        {
            return ReportingErrors.InvalidMonth(month);
        }

        var months = await _ledger.GetMonthlyCashflowAsync(year, ct);
        var trend = months
            .Select(m =>
            {
                var net = m.Income - m.Expense;
                var rate = m.Income > 0 ? Math.Round(net / m.Income * 100m, 1) : 0m;
                return new MonthlyCashflowDto(m.Month, m.Income, m.Expense, net, rate);
            })
            .ToList();

        var selected = trend.First(m => m.Month == month);
        return new CashflowReport(year, month, selected.Income, selected.Expense, selected.Net, trend);
    }

    /// <summary>Yearly category × month spending matrix (twelve fixed month columns).</summary>
    public async Task<Result<OverviewReport>> GetOverviewAsync(int year, CancellationToken ct = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var totals = await _ledger.GetExpenseTotalsByCategoryAsync(from, to, ReportGranularity.Monthly, ct);

        var rows = totals
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var months = new decimal[12];
                foreach (var t in g)
                {
                    months[t.PeriodStart.Month - 1] += t.Amount;
                }

                return new OverviewRow(g.Key, months, months.Sum());
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        var monthlyTotals = Enumerable.Range(0, 12)
            .Select(i => rows.Sum(r => r.Months[i]))
            .ToList();

        return new OverviewReport(year, rows, monthlyTotals, monthlyTotals.Sum());
    }

    /// <summary>Expense or income pivot of categories against period buckets over an inclusive date range.</summary>
    public async Task<Result<CategoryTrackerReport>> GetCategoryTrackerAsync(
        string? granularity, DateOnly from, DateOnly to, string? flow = null, CancellationToken ct = default)
    {
        if (!TryParseGranularity(granularity, out var grain))
        {
            return ReportingErrors.InvalidGranularity(granularity!);
        }

        if (from > to)
        {
            return ReportingErrors.InvalidDateRange;
        }

        var reportFlow = string.Equals(flow, "Income", StringComparison.OrdinalIgnoreCase)
            ? ReportFlow.Income
            : ReportFlow.Expense;

        var totals = await _ledger.GetCategoryTotalsAsync(from, to, reportFlow, grain, ct);

        // Period columns are the buckets that actually carry activity, in chronological order.
        var periods = totals
            .Select(t => t.PeriodStart)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        var index = periods.Select((d, i) => (d, i)).ToDictionary(x => x.d, x => x.i);

        var rows = totals
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var amounts = new decimal[periods.Count];
                foreach (var t in g)
                {
                    amounts[index[t.PeriodStart]] += t.Amount;
                }

                return new CategoryTrackerRow(g.Key, amounts, amounts.Sum());
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        var periodTotals = Enumerable.Range(0, periods.Count)
            .Select(i => rows.Sum(r => r.Amounts[i]))
            .ToList();

        return new CategoryTrackerReport(
            grain.ToString(), reportFlow.ToString(), from, to, periods, rows, periodTotals, periodTotals.Sum());
    }

    /// <summary>
    /// Computes executive decision support metrics: savings rate, emergency runway, daily burn rate,
    /// month-end projection, and Month-over-Month (MoM) growth rates.
    /// </summary>
    public async Task<Result<FinancialHealthReport>> GetFinancialHealthAsync(
        int year, int month, CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
        {
            return ReportingErrors.InvalidMonth(month);
        }

        // 1. Current year cashflow
        var monthsThisYear = await _ledger.GetMonthlyCashflowAsync(year, ct);
        var currentMonth = monthsThisYear.FirstOrDefault(m => m.Month == month)
            ?? new MonthlyCashflow(month, 0m, 0m);

        var net = currentMonth.Income - currentMonth.Expense;
        var savingsRate = currentMonth.Income > 0
            ? Math.Round(net / currentMonth.Income * 100m, 1)
            : (currentMonth.Expense > 0 ? -100m : 0m);

        // 2. Previous month for MoM comparison
        MonthlyCashflow? prevMonth = null;
        if (month > 1)
        {
            prevMonth = monthsThisYear.FirstOrDefault(m => m.Month == month - 1);
        }
        else
        {
            var monthsPrevYear = await _ledger.GetMonthlyCashflowAsync(year - 1, ct);
            prevMonth = monthsPrevYear.FirstOrDefault(m => m.Month == 12);
        }

        decimal? prevSavingsRate = null;
        decimal? momIncomePercent = null;
        decimal? momExpensePercent = null;
        decimal? momNetPercent = null;

        if (prevMonth is not null)
        {
            var prevNet = prevMonth.Income - prevMonth.Expense;
            prevSavingsRate = prevMonth.Income > 0
                ? Math.Round(prevNet / prevMonth.Income * 100m, 1)
                : 0m;

            if (prevMonth.Income > 0)
            {
                momIncomePercent = Math.Round((currentMonth.Income - prevMonth.Income) / prevMonth.Income * 100m, 1);
            }
            if (prevMonth.Expense > 0)
            {
                momExpensePercent = Math.Round((currentMonth.Expense - prevMonth.Expense) / prevMonth.Expense * 100m, 1);
            }
            if (prevNet != 0)
            {
                momNetPercent = Math.Round((net - prevNet) / Math.Abs(prevNet) * 100m, 1);
            }
        }

        // 3. Trailing 3-month average expense for emergency runway
        var trailingExpenses = new List<decimal>();
        for (var i = 1; i <= 3; i++)
        {
            var targetM = month - i;
            var targetY = year;
            if (targetM < 1)
            {
                targetM += 12;
                targetY -= 1;
            }

            IReadOnlyList<MonthlyCashflow> series = targetY == year
                ? monthsThisYear
                : await _ledger.GetMonthlyCashflowAsync(targetY, ct);

            var m = series.FirstOrDefault(x => x.Month == targetM);
            if (m is not null && m.Expense > 0)
            {
                trailingExpenses.Add(m.Expense);
            }
        }

        var trailing3Avg = trailingExpenses.Count > 0
            ? trailingExpenses.Average()
            : currentMonth.Expense;

        // 4. Liquid net worth & runway
        var balances = await _accounts.GetBalancesAsync(activeOnly: true, ct);
        var totalBalance = balances.Sum(b => b.Balance);

        var runwayMonths = trailing3Avg > 0
            ? Math.Round(totalBalance / trailing3Avg, 1)
            : (currentMonth.Expense > 0 ? Math.Round(totalBalance / currentMonth.Expense, 1) : 99m);

        // 5. Daily burn rate and month-end projection
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDaysInMonth = DateTime.DaysInMonth(year, month);
        int daysPassed;
        if (year == today.Year && month == today.Month)
        {
            daysPassed = Math.Max(1, today.Day);
        }
        else if (new DateOnly(year, month, 1) < new DateOnly(today.Year, today.Month, 1))
        {
            daysPassed = totalDaysInMonth;
        }
        else
        {
            daysPassed = 1;
        }

        var dailyBurnRate = daysPassed > 0
            ? Math.Round(currentMonth.Expense / daysPassed, 0)
            : 0m;

        var projectedMonthEndExpense = Math.Round(dailyBurnRate * totalDaysInMonth, 0);

        // 6. Health status
        var status = savingsRate switch
        {
            >= 30m => "Excellent",
            >= 20m => "Healthy",
            >= 0m => "Low",
            _ => "Deficit",
        };

        return new FinancialHealthReport(
            year,
            month,
            savingsRate,
            prevSavingsRate,
            runwayMonths,
            Math.Round(trailing3Avg, 0),
            dailyBurnRate,
            projectedMonthEndExpense,
            daysPassed,
            totalDaysInMonth,
            momIncomePercent,
            momExpensePercent,
            momNetPercent,
            status);
    }

    /// <summary>
    /// Spending distribution across top-level budget envelopes (Needs, Wants, Investment, Unclassified).
    /// </summary>
    public async Task<Result<EnvelopeReport>> GetEnvelopeReportAsync(
        int year, int? month, CancellationToken ct = default)
    {
        DateOnly from;
        DateOnly to;

        if (month.HasValue)
        {
            if (month.Value is < 1 or > 12)
            {
                return ReportingErrors.InvalidMonth(month.Value);
            }

            from = new DateOnly(year, month.Value, 1);
            to = new DateOnly(year, month.Value, DateTime.DaysInMonth(year, month.Value));
        }
        else
        {
            from = new DateOnly(year, 1, 1);
            to = new DateOnly(year, 12, 31);
        }

        var totals = await _ledger.GetEnvelopeTotalsAsync(from, to, ReportGranularity.Monthly, ct);
        var grouped = totals
            .GroupBy(t => t.BudgetCategoryId)
            .Select(g => new { BudgetCategoryId = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalExpense = grouped.Sum(x => x.Amount);

        var items = grouped.Select(g =>
        {
            var percent = totalExpense > 0
                ? Math.Round(g.Amount / totalExpense * 100m, 1)
                : 0m;

            return new EnvelopeItemDto(g.BudgetCategoryId, g.Amount, percent);
        }).ToList();

        return new EnvelopeReport(year, month, totalExpense, items);
    }

    private static bool TryParseGranularity(string? value, out ReportGranularity granularity)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            granularity = ReportGranularity.Monthly;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out granularity) && Enum.IsDefined(granularity);
    }
}
