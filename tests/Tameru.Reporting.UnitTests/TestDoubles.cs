using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Reporting.UnitTests;

/// <summary>Account balances stub. Returns the configured accounts, filtering inactive when asked.</summary>
internal sealed class FakeAccountBalanceDirectory : IAccountBalanceDirectory
{
    private readonly IReadOnlyList<AccountBalance> _accounts;

    public FakeAccountBalanceDirectory(params AccountBalance[] accounts) => _accounts = accounts;

    public Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(
        bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AccountBalance> result = activeOnly
            ? _accounts.Where(a => a.IsActive).ToList()
            : _accounts.ToList();
        return Task.FromResult(result);
    }
}

/// <summary>Ledger reporting-query stub driven by in-memory seed data.</summary>
internal sealed class FakeLedgerReportingQuery : ILedgerReportingQuery
{
    private readonly IReadOnlyList<MonthlyCashflow> _cashflow;
    private readonly IReadOnlyList<CategoryPeriodTotal> _categoryTotals;

    public FakeLedgerReportingQuery(
        IReadOnlyList<MonthlyCashflow>? cashflow = null,
        IReadOnlyList<CategoryPeriodTotal>? categoryTotals = null)
    {
        _cashflow = cashflow ?? Enumerable.Range(1, 12).Select(m => new MonthlyCashflow(m, 0, 0)).ToList();
        _categoryTotals = categoryTotals ?? [];
    }

    public Task<IReadOnlyList<MonthlyCashflow>> GetMonthlyCashflowAsync(
        int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cashflow);

    public Task<IReadOnlyList<CategoryPeriodTotal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryPeriodTotal> result = _categoryTotals
            .Where(t => t.PeriodStart >= from && t.PeriodStart <= to)
            .ToList();
        return Task.FromResult(result);
    }
}
