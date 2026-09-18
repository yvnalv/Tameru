using System.Security.Cryptography;
using System.Text;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.Modules.Contracts.Identity;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Time;

namespace Tameru.Reporting.Application;

/// <summary>
/// Generates proactive financial insights by analyzing spending patterns, budget pacing,
/// savings trends, and cash health indicators. All data is computed on read from existing
/// contracts — no new tables are introduced.
/// </summary>
public sealed class InsightsService
{
    private readonly IAccountBalanceDirectory _balances;
    private readonly IUserPreferences _userPreferences;
    private readonly IBudgetDecisionQuery _budgetDecision;
    private readonly ILedgerReportingQuery _ledger;
    private readonly DecisionService _decision;
    private readonly IClock _clock;

    public InsightsService(
        IAccountBalanceDirectory balances,
        IUserPreferences userPreferences,
        IBudgetDecisionQuery budgetDecision,
        ILedgerReportingQuery ledger,
        DecisionService decision,
        IClock clock)
    {
        _balances = balances;
        _userPreferences = userPreferences;
        _budgetDecision = budgetDecision;
        _ledger = ledger;
        _decision = decision;
        _clock = clock;
    }

    /// <summary>
    /// Computes and returns a prioritized list of proactive insights for the current period.
    /// </summary>
    public async Task<IReadOnlyList<InsightDto>> GetInsightsAsync(
        InsightsRequest? request = null, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var today = _clock.Today;
        var year = today.Year;
        var month = today.Month;
        var maxResults = request?.MaxResults ?? 10;

        var insights = new List<InsightDto>();

        // Run all insight generators in parallel where possible
        var safeTask = _decision.GetSafeToSpendAsync(ct);
        var cashflowTask = _ledger.GetMonthlyCashflowAsync(year, ct);
        var prevCashflowTask = _ledger.GetMonthlyCashflowAsync(year - 1, ct);
        var startDayTask = _userPreferences.GetBudgetCycleStartDayAsync(ct);
        var balancesTask = _balances.GetBalancesAsync(activeOnly: true, ct);

        await Task.WhenAll(safeTask, cashflowTask, prevCashflowTask, startDayTask, balancesTask);

        var safe = safeTask.Result;
        var cashflowSeries = cashflowTask.Result;
        var prevYearSeries = prevCashflowTask.Result;
        var startDay = startDayTask.Result;
        var allBalances = balancesTask.Result;

        var currentMonth = cashflowSeries.FirstOrDefault(m => m.Month == month);
        var totalBalance = allBalances.Sum(b => b.Balance);

        // --- 1. Payday Countdown Nudge ---
        insights.Add(GeneratePaydayNudge(safe, now));

        // --- 2. Budget Pacing Warnings ---
        try
        {
            var categoryBudgets = await _budgetDecision.GetCategoryBudgetsAsync(year, month, startDay, ct);
            insights.AddRange(GenerateBudgetPacingInsights(categoryBudgets, today, now));
        }
        catch
        {
            // Budget data may not be available; skip gracefully
        }

        // --- 3. Spending Velocity Alert ---
        insights.AddRange(GenerateSpendingVelocityInsights(cashflowSeries, month, today, now));

        // --- 4. Savings Rate Trend ---
        insights.AddRange(GenerateSavingsRateTrend(cashflowSeries, prevYearSeries, month, now));

        // --- 5. Cash Runway Alert ---
        insights.AddRange(GenerateRunwayAlert(totalBalance, cashflowSeries, prevYearSeries, month, now));

        // --- 6. Anomaly Detection (large single transactions) ---
        try
        {
            var from = new DateOnly(year, month, 1);
            var to = today;
            var categoryTotals = await _ledger.GetExpenseTotalsByCategoryAsync(from, to, ReportGranularity.Daily, ct);

            // Compare with previous months for anomaly baseline
            var baselineFrom = today.AddMonths(-3);
            var baselineTo = from.AddDays(-1);
            if (baselineTo >= baselineFrom)
            {
                var baselineTotals = await _ledger.GetExpenseTotalsByCategoryAsync(
                    baselineFrom, baselineTo, ReportGranularity.Monthly, ct);
                insights.AddRange(GenerateAnomalyInsights(categoryTotals, baselineTotals, today, now));
            }
        }
        catch
        {
            // Category data unavailable; skip
        }

        // Sort by severity (critical > warning > info), then by generated time
        var severityOrder = new Dictionary<string, int>
        {
            ["critical"] = 0,
            ["warning"] = 1,
            ["info"] = 2,
        };

        var filtered = insights
            .Where(i => PassesSeverityFilter(i.Severity, request?.MinSeverity))
            .OrderBy(i => severityOrder.GetValueOrDefault(i.Severity, 3))
            .ThenByDescending(i => i.GeneratedAt)
            .Take(maxResults)
            .ToList();

        return filtered;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Insight generators
    // ─────────────────────────────────────────────────────────────────────────

    private static InsightDto GeneratePaydayNudge(SafeToSpendDto safe, DateTimeOffset now)
    {
        var severity = safe.DaysRemaining <= 3 ? "warning"
            : safe.DaysRemaining <= 7 ? "info"
            : "info";

        var dailyFormatted = $"Rp {safe.DailyAllowance:N0}".Replace(",", ".");
        var safeFormatted = $"Rp {safe.SafeToSpend:N0}".Replace(",", ".");

        return new InsightDto(
            Id: HashId($"payday-{safe.NextPayday}"),
            Type: "payday",
            Severity: severity,
            Title: $"{safe.DaysRemaining} days until payday",
            Message: $"{safeFormatted} safe to spend ({dailyFormatted}/day). Next payday: {safe.NextPayday:dd MMM yyyy}.",
            ActionRoute: null,
            Value: safe.SafeToSpend,
            CategoryName: null,
            GeneratedAt: now);
    }

    private static IEnumerable<InsightDto> GenerateBudgetPacingInsights(
        IReadOnlyList<CategoryBudgetStatus> budgets, DateOnly today, DateTimeOffset now)
    {
        foreach (var cat in budgets)
        {
            if (cat.Plan <= 0) continue;

            var usagePercent = cat.Actual / cat.Plan * 100m;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var dayProgress = (decimal)today.Day / daysInMonth * 100m;

            // Over budget
            if (usagePercent >= 100m)
            {
                var overAmount = cat.Actual - cat.Plan;
                yield return new InsightDto(
                    Id: HashId($"pacing-over-{cat.CategoryId}-{today.Year}-{today.Month}"),
                    Type: "budget_pacing",
                    Severity: "critical",
                    Title: $"{cat.CategoryName} is over budget",
                    Message: $"Spent Rp {cat.Actual:N0} of Rp {cat.Plan:N0} plan — over by Rp {overAmount:N0}. Consider reducing spend or reallocating from surplus categories.".Replace(",", "."),
                    ActionRoute: "/budget",
                    Value: usagePercent,
                    CategoryName: cat.CategoryName,
                    GeneratedAt: now);
            }
            // Approaching budget (90%+) and spending faster than time elapsed
            else if (usagePercent >= 90m)
            {
                yield return new InsightDto(
                    Id: HashId($"pacing-90-{cat.CategoryId}-{today.Year}-{today.Month}"),
                    Type: "budget_pacing",
                    Severity: "warning",
                    Title: $"{cat.CategoryName} nearly exhausted",
                    Message: $"{usagePercent:F0}% of budget used with {daysInMonth - today.Day} days remaining. Only Rp {cat.Leftover:N0} left.".Replace(",", "."),
                    ActionRoute: "/budget",
                    Value: usagePercent,
                    CategoryName: cat.CategoryName,
                    GeneratedAt: now);
            }
            // Warning threshold (80%+) and pace is ahead of time
            else if (usagePercent >= 80m && usagePercent > dayProgress + 10m)
            {
                yield return new InsightDto(
                    Id: HashId($"pacing-80-{cat.CategoryId}-{today.Year}-{today.Month}"),
                    Type: "budget_pacing",
                    Severity: "warning",
                    Title: $"{cat.CategoryName} spending is ahead of pace",
                    Message: $"{usagePercent:F0}% used but only {dayProgress:F0}% of the month has passed. Rp {cat.Leftover:N0} remains.".Replace(",", "."),
                    ActionRoute: "/budget",
                    Value: usagePercent,
                    CategoryName: cat.CategoryName,
                    GeneratedAt: now);
            }
        }
    }

    private static IEnumerable<InsightDto> GenerateSpendingVelocityInsights(
        IReadOnlyList<MonthlyCashflow> series, int currentMonth, DateOnly today, DateTimeOffset now)
    {
        var current = series.FirstOrDefault(m => m.Month == currentMonth);
        if (current is null || current.Expense <= 0) yield break;

        // Compare with previous month
        var prevMonth = currentMonth - 1;
        var prev = prevMonth >= 1 ? series.FirstOrDefault(m => m.Month == prevMonth) : null;
        if (prev is null || prev.Expense <= 0) yield break;

        // Normalize current month's expense by days elapsed
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var daysPassed = Math.Max(1, today.Day);
        var projectedExpense = current.Expense / daysPassed * daysInMonth;
        var velocityRatio = projectedExpense / prev.Expense;

        if (velocityRatio >= 1.3m) // 30%+ faster spending pace
        {
            var pctFaster = (velocityRatio - 1m) * 100m;
            yield return new InsightDto(
                Id: HashId($"velocity-{today.Year}-{today.Month}"),
                Type: "spending_velocity",
                Severity: velocityRatio >= 1.5m ? "warning" : "info",
                Title: "Spending pace is elevated",
                Message: $"You're on track to spend {pctFaster:F0}% more than last month. Projected: Rp {projectedExpense:N0} vs. Rp {prev.Expense:N0} last month.".Replace(",", "."),
                ActionRoute: "/reports",
                Value: velocityRatio * 100m,
                CategoryName: null,
                GeneratedAt: now);
        }
        else if (velocityRatio <= 0.7m && current.Expense > 0) // 30%+ slower — positive signal
        {
            var pctSlower = (1m - velocityRatio) * 100m;
            yield return new InsightDto(
                Id: HashId($"velocity-slow-{today.Year}-{today.Month}"),
                Type: "spending_velocity",
                Severity: "info",
                Title: "Spending pace is lower than usual",
                Message: $"Great news! You're spending {pctSlower:F0}% less than last month's pace. Keep it up!",
                ActionRoute: null,
                Value: velocityRatio * 100m,
                CategoryName: null,
                GeneratedAt: now);
        }
    }

    private static IEnumerable<InsightDto> GenerateSavingsRateTrend(
        IReadOnlyList<MonthlyCashflow> series,
        IReadOnlyList<MonthlyCashflow> prevYearSeries,
        int currentMonth,
        DateTimeOffset now)
    {
        // Compute rolling 3-month savings rates
        var rates = new List<decimal>();
        for (var i = 0; i < 3; i++)
        {
            var m = currentMonth - i;
            var yr = series;
            if (m < 1)
            {
                m += 12;
                yr = prevYearSeries;
            }

            var entry = yr.FirstOrDefault(x => x.Month == m);
            if (entry is not null && entry.Income > 0)
            {
                var rate = (entry.Income - entry.Expense) / entry.Income * 100m;
                rates.Add(rate);
            }
        }

        if (rates.Count < 2) yield break;

        var currentRate = rates[0];
        var prevRate = rates[1];
        var delta = currentRate - prevRate;

        if (Math.Abs(delta) >= 5m) // Meaningful change
        {
            var isImproving = delta > 0;
            yield return new InsightDto(
                Id: HashId($"savings-trend-{currentMonth}"),
                Type: "savings_trend",
                Severity: isImproving ? "info" : "warning",
                Title: isImproving ? "Savings rate improving" : "Savings rate declining",
                Message: isImproving
                    ? $"Your savings rate improved from {prevRate:F1}% to {currentRate:F1}% (+{delta:F1} pts). Excellent progress!"
                    : $"Your savings rate dropped from {prevRate:F1}% to {currentRate:F1}% ({delta:F1} pts). Review your spending patterns.",
                ActionRoute: "/reports",
                Value: currentRate,
                CategoryName: null,
                GeneratedAt: now);
        }
    }

    private static IEnumerable<InsightDto> GenerateRunwayAlert(
        decimal totalBalance,
        IReadOnlyList<MonthlyCashflow> series,
        IReadOnlyList<MonthlyCashflow> prevYearSeries,
        int currentMonth,
        DateTimeOffset now)
    {
        // Trailing 3-month average expense
        var expenses = new List<decimal>();
        for (var i = 1; i <= 3; i++)
        {
            var m = currentMonth - i;
            var yr = series;
            if (m < 1)
            {
                m += 12;
                yr = prevYearSeries;
            }

            var entry = yr.FirstOrDefault(x => x.Month == m);
            if (entry is not null && entry.Expense > 0)
            {
                expenses.Add(entry.Expense);
            }
        }

        if (expenses.Count == 0) yield break;

        var avgExpense = expenses.Average();
        var runwayMonths = avgExpense > 0 ? totalBalance / avgExpense : 99m;

        if (runwayMonths < 2m)
        {
            yield return new InsightDto(
                Id: HashId($"runway-critical-{currentMonth}"),
                Type: "runway",
                Severity: "critical",
                Title: "Cash runway is critically low",
                Message: $"At current spending levels, your cash reserves cover only {runwayMonths:F1} months. Consider reducing expenses or increasing income immediately.",
                ActionRoute: "/accounts",
                Value: runwayMonths,
                CategoryName: null,
                GeneratedAt: now);
        }
        else if (runwayMonths < 4m)
        {
            yield return new InsightDto(
                Id: HashId($"runway-low-{currentMonth}"),
                Type: "runway",
                Severity: "warning",
                Title: "Cash runway is getting low",
                Message: $"Your reserves cover {runwayMonths:F1} months of expenses. Building a 3-6 month emergency fund is recommended.",
                ActionRoute: "/accounts",
                Value: runwayMonths,
                CategoryName: null,
                GeneratedAt: now);
        }
    }

    private static IEnumerable<InsightDto> GenerateAnomalyInsights(
        IReadOnlyList<CategoryPeriodTotal> currentTotals,
        IReadOnlyList<CategoryPeriodTotal> baselineTotals,
        DateOnly today,
        DateTimeOffset now)
    {
        // Aggregate current month by category
        var currentByCategory = currentTotals
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        // Compute baseline monthly averages by category (over ~3 months)
        var baselineByCategory = baselineTotals
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g =>
            {
                var months = g.Select(x => x.PeriodStart.Month).Distinct().Count();
                return months > 0 ? g.Sum(x => x.Amount) / months : g.Sum(x => x.Amount);
            });

        foreach (var (categoryId, currentAmount) in currentByCategory)
        {
            if (!baselineByCategory.TryGetValue(categoryId, out var avgAmount) || avgAmount <= 0)
                continue;

            // Normalize for partial month
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var daysPassed = Math.Max(1, today.Day);
            var projectedAmount = currentAmount / daysPassed * daysInMonth;

            var ratio = projectedAmount / avgAmount;

            if (ratio >= 2.0m && projectedAmount > 100_000m) // 2x+ above average and meaningful amount
            {
                var pctAbove = (ratio - 1m) * 100m;
                yield return new InsightDto(
                    Id: HashId($"anomaly-{categoryId}-{today.Year}-{today.Month}"),
                    Type: "anomaly",
                    Severity: ratio >= 3.0m ? "critical" : "warning",
                    Title: "Unusual spending detected",
                    Message: $"Projected spending is {pctAbove:F0}% above the 3-month average (Rp {projectedAmount:N0} vs. avg Rp {avgAmount:N0}).".Replace(",", "."),
                    ActionRoute: "/transactions",
                    Value: projectedAmount,
                    CategoryName: null, // We don't have category names at this level
                    GeneratedAt: now);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool PassesSeverityFilter(string severity, string? minSeverity)
    {
        if (string.IsNullOrWhiteSpace(minSeverity)) return true;

        var severityLevel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["info"] = 0,
            ["warning"] = 1,
            ["critical"] = 2,
        };

        var currentLevel = severityLevel.GetValueOrDefault(severity, 0);
        var minLevel = severityLevel.GetValueOrDefault(minSeverity, 0);
        return currentLevel >= minLevel;
    }

    private static string HashId(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
