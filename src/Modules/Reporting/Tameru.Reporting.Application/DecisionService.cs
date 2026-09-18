using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.Modules.Contracts.Identity;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Results;
using Tameru.SharedKernel.Time;

namespace Tameru.Reporting.Application;

public sealed class DecisionService
{
    private static readonly HashSet<string> LiquidAccountTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash", "Bank", "EWallet", "E-Wallet", "Savings", "Giro"
    };

    private readonly IAccountBalanceDirectory _balances;
    private readonly IUserPreferences _userPreferences;
    private readonly IBudgetDecisionQuery _budgetDecision;
    private readonly IClock _clock;

    public DecisionService(
        IAccountBalanceDirectory balances,
        IUserPreferences userPreferences,
        IBudgetDecisionQuery budgetDecision,
        IClock clock)
    {
        _balances = balances;
        _userPreferences = userPreferences;
        _budgetDecision = budgetDecision;
        _clock = clock;
    }

    public async Task<SafeToSpendDto> GetSafeToSpendAsync(CancellationToken ct = default)
    {
        var startDay = await _userPreferences.GetBudgetCycleStartDayAsync(ct);
        var today = _clock.Today;
        var (cycleStart, cycleEnd, nextPayday, daysRemaining) = CalculateCycle(today, startDay);

        var allBalances = await _balances.GetBalancesAsync(activeOnly: true, ct);
        var liquidAccounts = allBalances
            .Where(a => LiquidAccountTypes.Contains(a.Type) && a.Balance > 0m)
            .Select(a => new LiquidAccountDto(a.Id, a.Name, a.Type, a.Balance))
            .ToList();

        var liquidCash = liquidAccounts.Sum(a => a.Balance);

        var fixedObligations = await _budgetDecision.GetFixedObligationsAsync(ct);
        var upcomingObligations = fixedObligations.Select(o =>
        {
            var dueDate = ResolveDueDate(o.DueDay, today, nextPayday);
            var isDue = dueDate >= today && dueDate <= nextPayday;
            return new ObligationItemDto(o.Id, o.Name, o.Amount, o.SectionName, dueDate, isDue);
        }).ToList();

        var unpaidObligations = upcomingObligations
            .Where(o => o.IsDueBeforePayday)
            .Sum(o => o.Amount);

        // Safety buffer: 5% of liquid cash to prevent zero-cash traps
        var safetyBuffer = Math.Round(Math.Max(0m, liquidCash) * 0.05m, 0);
        var safeToSpend = Math.Max(0m, liquidCash - unpaidObligations - safetyBuffer);
        var dailyAllowance = daysRemaining > 0 ? Math.Round(safeToSpend / daysRemaining, 0) : safeToSpend;

        return new SafeToSpendDto(
            liquidCash,
            unpaidObligations,
            safetyBuffer,
            safeToSpend,
            daysRemaining,
            dailyAllowance,
            cycleStart,
            cycleEnd,
            nextPayday,
            upcomingObligations,
            liquidAccounts);
    }

    public async Task<Result<SimulatePurchaseResultDto>> SimulatePurchaseAsync(
        SimulatePurchaseRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0m)
        {
            return new Error("simulator_amount_positive", "Simulation amount must be greater than zero.");
        }

        var startDay = await _userPreferences.GetBudgetCycleStartDayAsync(ct);
        var safe = await GetSafeToSpendAsync(ct);
        var newSafeToSpend = Math.Max(0m, safe.SafeToSpend - request.Amount);
        var newDailyAllowance = safe.DaysRemaining > 0
            ? Math.Round(newSafeToSpend / safe.DaysRemaining, 0)
            : newSafeToSpend;

        string verdict;
        string summary;
        string? categoryName = null;
        decimal? categoryPlan = null;
        decimal? categoryActual = null;
        decimal? categoryLeftover = null;
        decimal? categoryLeftoverAfterPurchase = null;
        var surplusCategories = new List<SurplusCategoryDto>();

        if (request.CategoryId.HasValue)
        {
            var categoryBudgets = await _budgetDecision.GetCategoryBudgetsAsync(
                safe.CycleStart.Year, safe.CycleStart.Month, startDay, ct);

            var targetCat = categoryBudgets.FirstOrDefault(c => c.CategoryId == request.CategoryId.Value);
            if (targetCat is not null)
            {
                categoryName = targetCat.CategoryName;
                categoryPlan = targetCat.Plan;
                categoryActual = targetCat.Actual;
                categoryLeftover = targetCat.Leftover;
                categoryLeftoverAfterPurchase = targetCat.Leftover - request.Amount;

                surplusCategories = categoryBudgets
                    .Where(c => c.CategoryId != request.CategoryId.Value && c.Leftover > 0m)
                    .OrderByDescending(c => c.Leftover)
                    .Select(c => new SurplusCategoryDto(c.CategoryId, c.CategoryName, c.Leftover))
                    .ToList();
            }

            if (request.Amount > safe.SafeToSpend)
            {
                verdict = "Risky";
                var excess = request.Amount - safe.SafeToSpend;
                summary = $"Risky: Exceeds safe uncommitted cash by {excess:N0}. May endanger obligations before payday on {safe.NextPayday:dd MMM yyyy}.";
            }
            else if (categoryLeftover.HasValue && categoryLeftover.Value > 0m && request.Amount > categoryLeftover.Value)
            {
                verdict = "Warning";
                var catExcess = request.Amount - categoryLeftover.Value;
                summary = $"Caution: Total cash is sufficient, but this exceeds your '{categoryName}' budget by {catExcess:N0}. Reallocate from surplus categories to maintain plan.";
            }
            else
            {
                verdict = "Safe";
                summary = $"Safe: Purchase fits comfortably. Daily allowance adjusts to {newDailyAllowance:N0}/day until payday on {safe.NextPayday:dd MMM yyyy}.";
            }
        }
        else
        {
            if (request.Amount > safe.SafeToSpend)
            {
                verdict = "Risky";
                var excess = request.Amount - safe.SafeToSpend;
                summary = $"Risky: Exceeds safe uncommitted cash by {excess:N0}. May endanger obligations before payday on {safe.NextPayday:dd MMM yyyy}.";
            }
            else
            {
                verdict = "Safe";
                summary = $"Safe: Purchase fits comfortably. Safe-to-spend leaves {newSafeToSpend:N0} ({newDailyAllowance:N0}/day).";
            }
        }

        return new SimulatePurchaseResultDto(
            verdict,
            request.Amount,
            request.CategoryId,
            categoryName,
            safe.SafeToSpend,
            newSafeToSpend,
            safe.DailyAllowance,
            newDailyAllowance,
            safe.DaysRemaining,
            categoryPlan,
            categoryActual,
            categoryLeftover,
            categoryLeftoverAfterPurchase,
            surplusCategories,
            summary);
    }

    public static (DateOnly CycleStart, DateOnly CycleEnd, DateOnly NextPayday, int DaysRemaining) CalculateCycle(
        DateOnly today, int startDay)
    {
        var clampedDay = Math.Clamp(startDay, 1, 28);
        DateOnly cycleStart;
        DateOnly nextPayday;

        if (clampedDay == 1)
        {
            cycleStart = new DateOnly(today.Year, today.Month, 1);
            nextPayday = cycleStart.AddMonths(1);
        }
        else if (today.Day >= clampedDay)
        {
            cycleStart = new DateOnly(today.Year, today.Month, clampedDay);
            nextPayday = cycleStart.AddMonths(1);
        }
        else
        {
            var prevMonth = today.AddMonths(-1);
            var prevMonthDays = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
            var day = Math.Min(clampedDay, prevMonthDays);
            cycleStart = new DateOnly(prevMonth.Year, prevMonth.Month, day);
            nextPayday = new DateOnly(today.Year, today.Month, clampedDay);
        }

        var cycleEnd = nextPayday.AddDays(-1);
        var daysRemaining = Math.Max(1, nextPayday.DayNumber - today.DayNumber);
        return (cycleStart, cycleEnd, nextPayday, daysRemaining);
    }

    private static DateOnly ResolveDueDate(int? dueDay, DateOnly today, DateOnly nextPayday)
    {
        if (dueDay is not (>= 1 and <= 28))
        {
            // Default to 1st of current month or nextPayday
            return nextPayday.AddDays(-5);
        }

        var candidate = new DateOnly(today.Year, today.Month, dueDay.Value);
        if (candidate < today)
        {
            // Next month's due day
            var next = today.AddMonths(1);
            return new DateOnly(next.Year, next.Month, Math.Min(dueDay.Value, DateTime.DaysInMonth(next.Year, next.Month)));
        }

        return candidate;
    }
}
