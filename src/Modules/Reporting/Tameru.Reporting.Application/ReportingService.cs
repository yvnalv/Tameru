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
            .Select(m => new MonthlyCashflowDto(m.Month, m.Income, m.Expense, m.Income - m.Expense))
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

    /// <summary>Expense pivot of categories against period buckets over an inclusive date range.</summary>
    public async Task<Result<CategoryTrackerReport>> GetCategoryTrackerAsync(
        string? granularity, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (!TryParseGranularity(granularity, out var grain))
        {
            return ReportingErrors.InvalidGranularity(granularity!);
        }

        if (from > to)
        {
            return ReportingErrors.InvalidDateRange;
        }

        var totals = await _ledger.GetExpenseTotalsByCategoryAsync(from, to, grain, ct);

        // Period columns are the buckets that actually carry spend, in chronological order.
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
            grain.ToString(), from, to, periods, rows, periodTotals, periodTotals.Sum());
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
