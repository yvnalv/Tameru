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
    private readonly IReadOnlyList<EnvelopePeriodTotal> _envelopeTotals;

    public FakeLedgerReportingQuery(
        IReadOnlyList<MonthlyCashflow>? cashflow = null,
        IReadOnlyList<CategoryPeriodTotal>? categoryTotals = null,
        IReadOnlyList<EnvelopePeriodTotal>? envelopeTotals = null)
    {
        _cashflow = cashflow ?? Enumerable.Range(1, 12).Select(m => new MonthlyCashflow(m, 0, 0)).ToList();
        _categoryTotals = categoryTotals ?? [];
        _envelopeTotals = envelopeTotals ?? [];
    }

    public Task<IReadOnlyList<MonthlyCashflow>> GetMonthlyCashflowAsync(
        int year, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cashflow);

    public Task<IReadOnlyList<CategoryPeriodTotal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default) =>
        GetCategoryTotalsAsync(from, to, ReportFlow.Expense, granularity, cancellationToken);

    public Task<IReadOnlyList<CategoryPeriodTotal>> GetCategoryTotalsAsync(
        DateOnly from, DateOnly to, ReportFlow flow, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryPeriodTotal> result = _categoryTotals
            .Where(t => t.PeriodStart >= from && t.PeriodStart <= to)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<EnvelopePeriodTotal>> GetEnvelopeTotalsAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EnvelopePeriodTotal> result = _envelopeTotals
            .Where(t => t.PeriodStart >= from && t.PeriodStart <= to)
            .ToList();
        return Task.FromResult(result);
    }
}

internal sealed class FakeUserPreferences : Tameru.Modules.Contracts.Identity.IUserPreferences
{
    private readonly int _startDay;

    public FakeUserPreferences(int startDay = 1) => _startDay = startDay;

    public Task<int> GetBudgetCycleStartDayAsync(CancellationToken ct = default) => Task.FromResult(_startDay);
}

internal sealed class FakeBudgetDecisionQuery : Tameru.Modules.Contracts.Budgeting.IBudgetDecisionQuery
{
    private readonly IReadOnlyList<Tameru.Modules.Contracts.Budgeting.CategoryBudgetStatus> _categories;
    private readonly IReadOnlyList<Tameru.Modules.Contracts.Budgeting.FixedObligationRef> _obligations;

    public FakeBudgetDecisionQuery(
        IReadOnlyList<Tameru.Modules.Contracts.Budgeting.CategoryBudgetStatus>? categories = null,
        IReadOnlyList<Tameru.Modules.Contracts.Budgeting.FixedObligationRef>? obligations = null)
    {
        _categories = categories ?? [];
        _obligations = obligations ?? [];
    }

    public Task<IReadOnlyList<Tameru.Modules.Contracts.Budgeting.CategoryBudgetStatus>> GetCategoryBudgetsAsync(
        int year, int month, int startDay = 1, CancellationToken ct = default) =>
        Task.FromResult(_categories);

    public Task<IReadOnlyList<Tameru.Modules.Contracts.Budgeting.FixedObligationRef>> GetFixedObligationsAsync(
        CancellationToken ct = default) =>
        Task.FromResult(_obligations);
}

