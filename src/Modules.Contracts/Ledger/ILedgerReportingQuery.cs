namespace Tameru.Modules.Contracts.Ledger;

/// <summary>The bucket size for a category pivot (docs/API_SPEC.md → Reporting).</summary>
public enum ReportGranularity
{
    Daily,
    Monthly,
}

public enum ReportFlow
{
    Expense,
    Income,
}

/// <summary>
/// Aggregate ledger reads exposed for the Reporting module. The ledger is the single source of truth
/// (ADR-0006); Reporting owns no data and never queries ledger tables directly. Voided transactions
/// are excluded (BR-007). Provided by the Ledger module.
/// </summary>
public interface ILedgerReportingQuery
{
    /// <summary>
    /// Income and expense totals for each calendar month (1..12) of <paramref name="year"/>. Months
    /// with no activity are returned as zero so callers get a complete 12-point series.
    /// </summary>
    Task<IReadOnlyList<MonthlyCashflow>> GetMonthlyCashflowAsync(
        int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Expense totals grouped by the classifying <c>Category</c> (level 2) and by period bucket over
    /// the inclusive range <paramref name="from"/>..<paramref name="to"/>. Only buckets and categories
    /// with spend are returned; the caller pivots them into a matrix. Expenses without a level-2
    /// category are excluded.
    /// </summary>
    Task<IReadOnlyList<CategoryPeriodTotal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Category totals grouped by the classifying <c>Category</c> (level 2) and by period bucket over
    /// the inclusive range <paramref name="from"/>..<paramref name="to"/> for the specified flow (<paramref name="flow"/>).
    /// </summary>
    Task<IReadOnlyList<CategoryPeriodTotal>> GetCategoryTotalsAsync(
        DateOnly from, DateOnly to, ReportFlow flow, ReportGranularity granularity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expense totals grouped by Level-1 <c>BudgetCategory</c> (Needs, Wants, Investment) over
    /// the inclusive range <paramref name="from"/>..<paramref name="to"/>.
    /// </summary>
    Task<IReadOnlyList<EnvelopePeriodTotal>> GetEnvelopeTotalsAsync(
        DateOnly from, DateOnly to, ReportGranularity granularity,
        CancellationToken cancellationToken = default);
}

/// <summary>Income vs. expense for one calendar month.</summary>
public sealed record MonthlyCashflow(int Month, decimal Income, decimal Expense);

/// <summary>Category total for one category within one period bucket.</summary>
/// <param name="CategoryId">The level-2 category id.</param>
/// <param name="PeriodStart">The bucket's first day (the day itself for Daily, the 1st for Monthly).</param>
/// <param name="Amount">Sum of non-voided transaction amounts in the bucket for the category.</param>
public sealed record CategoryPeriodTotal(Guid CategoryId, DateOnly PeriodStart, decimal Amount);

/// <summary>Spend total for one Level-1 budget envelope within one period bucket.</summary>
/// <param name="BudgetCategoryId">The level-1 budget category id (null if unclassified).</param>
/// <param name="PeriodStart">The bucket's first day.</param>
/// <param name="Amount">Sum of non-voided expense amounts in the bucket.</param>
public sealed record EnvelopePeriodTotal(Guid? BudgetCategoryId, DateOnly PeriodStart, decimal Amount);
