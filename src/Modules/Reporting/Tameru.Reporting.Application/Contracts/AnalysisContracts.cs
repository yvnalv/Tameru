namespace Tameru.Reporting.Application.Contracts;

public sealed record DeepAnalysisDto(
    AnalysisPeriodDto Period,
    IncomeAnalysisDto Income,
    ExpenseAnalysisDto Expense);

public sealed record AnalysisPeriodDto(
    int Year,
    int Month,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record IncomeAnalysisDto(
    decimal TotalIncome,
    decimal PreviousPeriodIncome,
    decimal? MoMChangePercent,
    decimal RetentionRate,
    int StabilityScore,
    IReadOnlyList<IncomeCategoryItemDto> Categories);

public sealed record IncomeCategoryItemDto(
    Guid CategoryId,
    decimal Amount,
    decimal Percentage);

public sealed record ExpenseAnalysisDto(
    decimal TotalExpense,
    decimal PreviousPeriodExpense,
    decimal? MoMChangePercent,
    FixedVsVariableDto FixedVsVariable,
    WeekdayVsWeekendDto WeekdayVsWeekend,
    IReadOnlyList<PayeeItemDto> TopPayees,
    IReadOnlyList<CategoryMomentumDto> CategoryMomentum);

public sealed record FixedVsVariableDto(
    decimal FixedAmount,
    decimal VariableAmount,
    decimal FixedPercentage,
    decimal VariablePercentage);

public sealed record WeekdayVsWeekendDto(
    decimal WeekdayTotal,
    decimal WeekdayDailyAverage,
    decimal WeekendTotal,
    decimal WeekendDailyAverage,
    decimal WeekendVelocityRatio,
    IReadOnlyList<DayOfWeekItemDto> DayOfWeekBreakdown);

public sealed record DayOfWeekItemDto(
    string DayOfWeek,
    decimal TotalAmount,
    int DayCount,
    decimal AveragePerDay);

public sealed record PayeeItemDto(
    string Payee,
    decimal Amount,
    int TransactionCount,
    decimal Percentage);

public sealed record CategoryMomentumDto(
    Guid CategoryId,
    decimal CurrentAmount,
    decimal TrailingAverageAmount,
    decimal GrowthPercent);

public sealed record RecommendationDto(
    string Id,
    string Strategy,
    string Severity,
    string Title,
    string Summary,
    string ActionText,
    string ActionRoute,
    decimal? EstimatedMonthlySavings);
