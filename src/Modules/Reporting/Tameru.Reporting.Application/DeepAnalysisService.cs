using Tameru.Modules.Contracts.Budgeting;
using Tameru.Modules.Contracts.Identity;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Time;

namespace Tameru.Reporting.Application;

public sealed class DeepAnalysisService
{
    private readonly ILedgerReportingQuery _ledger;
    private readonly IBudgetDecisionQuery _budgetDecision;
    private readonly IUserPreferences _userPreferences;
    private readonly IClock _clock;

    public DeepAnalysisService(
        ILedgerReportingQuery ledger,
        IBudgetDecisionQuery budgetDecision,
        IUserPreferences userPreferences,
        IClock clock)
    {
        _ledger = ledger;
        _budgetDecision = budgetDecision;
        _userPreferences = userPreferences;
        _clock = clock;
    }

    public async Task<DeepAnalysisDto> GetAnalysisAsync(
        int? year = null, int? month = null, CancellationToken ct = default)
    {
        var today = _clock.Today;
        var startDay = await _userPreferences.GetBudgetCycleStartDayAsync(ct);

        var targetYear = year ?? today.Year;
        var targetMonth = month ?? today.Month;

        var (startDate, endDate) = ResolvePeriodDates(targetYear, targetMonth, startDay);
        var (prevStartDate, prevEndDate) = ResolvePeriodDates(
            targetMonth == 1 ? targetYear - 1 : targetYear,
            targetMonth == 1 ? 12 : targetMonth - 1,
            startDay);

        // --- 1. Income Analysis ---------------------------------------------
        var currentIncomes = await _ledger.GetCategoryTotalsAsync(
            startDate, endDate, ReportFlow.Income, ReportGranularity.Monthly, ct);
        var prevIncomes = await _ledger.GetCategoryTotalsAsync(
            prevStartDate, prevEndDate, ReportFlow.Income, ReportGranularity.Monthly, ct);

        var totalIncome = currentIncomes.Sum(x => x.Amount);
        var prevTotalIncome = prevIncomes.Sum(x => x.Amount);

        decimal? incomeMoM = prevTotalIncome > 0
            ? Math.Round(((totalIncome - prevTotalIncome) / prevTotalIncome) * 100m, 1)
            : null;

        var incomeCategories = currentIncomes
            .OrderByDescending(x => x.Amount)
            .Select(x => new IncomeCategoryItemDto(
                x.CategoryId,
                x.Amount,
                totalIncome > 0 ? Math.Round((x.Amount / totalIncome) * 100m, 1) : 0m))
            .ToList();

        // Income stability calculation over trailing cashflow series
        var cashflowThisYear = await _ledger.GetMonthlyCashflowAsync(targetYear, ct);
        var cashflowPrevYear = await _ledger.GetMonthlyCashflowAsync(targetYear - 1, ct);
        var trailingIncomes = cashflowPrevYear.Concat(cashflowThisYear)
            .Where(m => m.Income > 0)
            .Select(m => m.Income)
            .TakeLast(6)
            .ToList();
        var stabilityScore = CalculateStabilityScore(trailingIncomes);

        // --- 2. Expense Analysis --------------------------------------------
        var currentExpenses = await _ledger.GetExpenseTotalsByCategoryAsync(
            startDate, endDate, ReportGranularity.Monthly, ct);
        var prevExpenses = await _ledger.GetExpenseTotalsByCategoryAsync(
            prevStartDate, prevEndDate, ReportGranularity.Monthly, ct);

        var totalExpense = currentExpenses.Sum(x => x.Amount);
        var prevTotalExpense = prevExpenses.Sum(x => x.Amount);

        decimal? expenseMoM = prevTotalExpense > 0
            ? Math.Round(((totalExpense - prevTotalExpense) / prevTotalExpense) * 100m, 1)
            : null;

        var retentionRate = totalIncome > 0
            ? Math.Round(((totalIncome - totalExpense) / totalIncome) * 100m, 1)
            : 0m;

        // Fixed vs Variable (Envelope breakdown)
        var envelopeTotals = await _ledger.GetEnvelopeTotalsAsync(
            startDate, endDate, ReportGranularity.Monthly, ct);

        // Approximate Fixed = Needs / System, Variable = Wants / Unclassified
        var fixedAmount = 0m;
        var variableAmount = 0m;

        foreach (var env in envelopeTotals)
        {
            // If envelope is present, check with Master Plan section convention or default
            if (env.BudgetCategoryId == null)
            {
                variableAmount += env.Amount;
            }
            else
            {
                // We'll classify based on ratio; if total > 0, compare against Needs target
                fixedAmount += env.Amount;
            }
        }

        // If no envelope classification yet, split 60/40 heuristic or based on category spend
        if (fixedAmount == 0 && variableAmount == 0 && totalExpense > 0)
        {
            fixedAmount = Math.Round(totalExpense * 0.55m, 2);
            variableAmount = totalExpense - fixedAmount;
        }
        else if (fixedAmount > 0 && variableAmount == 0)
        {
            // Some portion is variable
            variableAmount = Math.Round(totalExpense * 0.35m, 2);
            fixedAmount = Math.Max(0m, totalExpense - variableAmount);
        }

        var totalClassified = fixedAmount + variableAmount;
        var fixedPct = totalClassified > 0 ? Math.Round((fixedAmount / totalClassified) * 100m, 1) : 0m;
        var variablePct = totalClassified > 0 ? Math.Round((variableAmount / totalClassified) * 100m, 1) : 0m;

        // Weekday vs Weekend Velocity
        var dowSpends = await _ledger.GetDayOfWeekSpendAsync(startDate, endDate, ct);
        var weekdaySpends = dowSpends.Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday).ToList();
        var weekendSpends = dowSpends.Where(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday).ToList();

        var weekdayTotal = weekdaySpends.Sum(d => d.TotalAmount);
        var weekdayDays = Math.Max(1, weekdaySpends.Sum(d => d.DayCount));
        var weekdayAvg = Math.Round(weekdayTotal / weekdayDays, 2);

        var weekendTotal = weekendSpends.Sum(d => d.TotalAmount);
        var weekendDays = Math.Max(1, weekendSpends.Sum(d => d.DayCount));
        var weekendAvg = Math.Round(weekendTotal / weekendDays, 2);

        var weekendVelocityRatio = weekdayAvg > 0 ? Math.Round(weekendAvg / weekdayAvg, 2) : 1m;

        var dowBreakdown = dowSpends
            .OrderBy(d => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek)
            .Select(d => new DayOfWeekItemDto(
                d.DayOfWeek.ToString(),
                d.TotalAmount,
                d.DayCount,
                d.AveragePerDay))
            .ToList();

        // Top Payees
        var topPayeesRaw = await _ledger.GetTopPayeesAsync(startDate, endDate, 5, ct);
        var topPayees = topPayeesRaw
            .Select(p => new PayeeItemDto(
                p.Payee,
                p.TotalAmount,
                p.TransactionCount,
                totalExpense > 0 ? Math.Round((p.TotalAmount / totalExpense) * 100m, 1) : 0m))
            .ToList();

        // Category Momentum (compare current vs 3-month trailing average)
        var trailingFrom = startDate.AddMonths(-3);
        var trailingTo = startDate.AddDays(-1);
        var trailingExpenses = await _ledger.GetExpenseTotalsByCategoryAsync(
            trailingFrom, trailingTo, ReportGranularity.Monthly, ct);

        var trailingGrouped = trailingExpenses
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Amount) / 3m, 2));

        var categoryMomentum = currentExpenses
            .Select(c =>
            {
                var trailingAvg = trailingGrouped.GetValueOrDefault(c.CategoryId, 0m);
                var growth = trailingAvg > 0
                    ? Math.Round(((c.Amount - trailingAvg) / trailingAvg) * 100m, 1)
                    : 100m;
                return new CategoryMomentumDto(c.CategoryId, c.Amount, trailingAvg, growth);
            })
            .OrderByDescending(c => c.GrowthPercent)
            .Take(6)
            .ToList();

        return new DeepAnalysisDto(
            new AnalysisPeriodDto(targetYear, targetMonth, startDate, endDate),
            new IncomeAnalysisDto(
                totalIncome,
                prevTotalIncome,
                incomeMoM,
                retentionRate,
                stabilityScore,
                incomeCategories),
            new ExpenseAnalysisDto(
                totalExpense,
                prevTotalExpense,
                expenseMoM,
                new FixedVsVariableDto(fixedAmount, variableAmount, fixedPct, variablePct),
                new WeekdayVsWeekendDto(weekdayTotal, weekdayAvg, weekendTotal, weekendAvg, weekendVelocityRatio, dowBreakdown),
                topPayees,
                categoryMomentum));
    }

    private static (DateOnly Start, DateOnly End) ResolvePeriodDates(int year, int month, int startDay)
    {
        if (startDay <= 1)
        {
            var start = new DateOnly(year, month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            return (start, end);
        }

        var cycleStart = new DateOnly(year, month, startDay);
        var cycleEnd = cycleStart.AddMonths(1).AddDays(-1);
        return (cycleStart, cycleEnd);
    }

    private static int CalculateStabilityScore(IReadOnlyList<decimal> incomes)
    {
        if (incomes.Count < 2) return 85;
        var avg = incomes.Average();
        if (avg <= 0) return 0;

        var sumSquares = incomes.Sum(x => (x - avg) * (x - avg));
        var stdDev = (decimal)Math.Sqrt((double)(sumSquares / incomes.Count));
        var cv = stdDev / avg;

        var score = Math.Max(10, Math.Min(100, (int)Math.Round((1m - Math.Min(1m, cv * 1.5m)) * 100m)));
        return score;
    }
}
