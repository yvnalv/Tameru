namespace Tameru.Reporting.Application.Contracts;

// --- Net worth --------------------------------------------------------------

/// <summary>Net worth = sum of derived balances over active accounts (BR-023), plus the breakdown.</summary>
public sealed record NetWorthReport(
    decimal Total,
    string CurrencyCode,
    IReadOnlyList<AccountBalanceDto> Accounts);

/// <summary>One account's derived balance in the net-worth breakdown.</summary>
public sealed record AccountBalanceDto(
    Guid AccountId,
    string Name,
    string? GroupName,
    string Type,
    decimal Balance,
    string CurrencyCode);

// --- Cashflow ---------------------------------------------------------------

/// <summary>
/// Income vs. expense for a selected month plus the 12-month trend for its year
/// (docs/API_SPEC.md → cashflow).
/// </summary>
public sealed record CashflowReport(
    int Year,
    int Month,
    decimal Income,
    decimal Expense,
    decimal Net,
    IReadOnlyList<MonthlyCashflowDto> Trend);

/// <summary>One month of the cashflow trend.</summary>
public sealed record MonthlyCashflowDto(
    int Month, decimal Income, decimal Expense, decimal Net, decimal SavingsRate);

// --- Overview (yearly category × month matrix) ------------------------------

/// <summary>Yearly spending matrix: a row per category, twelve month columns (docs/API_SPEC.md → overview).</summary>
public sealed record OverviewReport(
    int Year,
    IReadOnlyList<OverviewRow> Categories,
    IReadOnlyList<decimal> MonthlyTotals,
    decimal Total);

/// <summary>One category's monthly spend across the year (<see cref="Months"/> has 12 entries).</summary>
public sealed record OverviewRow(Guid CategoryId, IReadOnlyList<decimal> Months, decimal Total);

// --- Category tracker (period pivot) ----------------------------------------

/// <summary>
/// Pivot: categories against period buckets over a date range (docs/API_SPEC.md → tracker).
/// Only buckets with spend appear in <see cref="Periods"/>; each row aligns to that order.
/// </summary>
public sealed record CategoryTrackerReport(
    string Granularity,
    string Flow,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DateOnly> Periods,
    IReadOnlyList<CategoryTrackerRow> Categories,
    IReadOnlyList<decimal> PeriodTotals,
    decimal Total);

/// <summary>One category's spend across the tracker's periods (<see cref="Amounts"/> aligns to Periods).</summary>
public sealed record CategoryTrackerRow(Guid CategoryId, IReadOnlyList<decimal> Amounts, decimal Total);

// --- Financial health & decision support ------------------------------------

/// <summary>
/// Executive decision support metrics: savings rate, financial runway, daily burn rate,
/// month-end projection, and Month-over-Month (MoM) deltas.
/// </summary>
public sealed record FinancialHealthReport(
    int Year,
    int Month,
    decimal SavingsRate,
    decimal? PreviousSavingsRate,
    decimal RunwayMonths,
    decimal Trailing3MonthAvgExpense,
    decimal DailyBurnRate,
    decimal ProjectedMonthEndExpense,
    int DaysPassed,
    int TotalDaysInMonth,
    decimal? MomIncomePercent,
    decimal? MomExpensePercent,
    decimal? MomNetPercent,
    string HealthStatus);

// --- Allocation Envelopes (Needs / Wants / Investment) -----------------------

/// <summary>
/// Spending distributed by top-level budget envelope (Needs, Wants, Investment, and Unclassified).
/// </summary>
public sealed record EnvelopeReport(
    int Year,
    int? Month,
    decimal TotalExpense,
    IReadOnlyList<EnvelopeItemDto> Envelopes);

public sealed record EnvelopeItemDto(
    Guid? BudgetCategoryId,
    decimal Amount,
    decimal Percent);
