using Tameru.Modules.Contracts.Accounts;
using Tameru.Reporting.Application.Contracts;

namespace Tameru.Reporting.Application;

public sealed class RecommendationService
{
    private readonly DeepAnalysisService _analysis;
    private readonly IAccountBalanceDirectory _balances;

    public RecommendationService(
        DeepAnalysisService analysis,
        IAccountBalanceDirectory balances)
    {
        _analysis = analysis;
        _balances = balances;
    }

    public async Task<IReadOnlyList<RecommendationDto>> GetRecommendationsAsync(
        int? year = null, int? month = null, CancellationToken ct = default)
    {
        var analysis = await _analysis.GetAnalysisAsync(year, month, ct);
        var activeBalances = await _balances.GetBalancesAsync(activeOnly: true, ct);
        var liquidCash = activeBalances
            .Where(b => b.Balance > 0 && IsLiquid(b.Type))
            .Sum(b => b.Balance);

        var recommendations = new List<RecommendationDto>();

        // 1. Weekend Burn Velocity Recommendation
        var weekend = analysis.Expense.WeekdayVsWeekend;
        if (weekend.WeekendVelocityRatio >= 1.8m && weekend.WeekendDailyAverage >= 150_000m)
        {
            var potentialMonthlySavings = Math.Max(0m, Math.Round((weekend.WeekendDailyAverage - weekend.WeekdayDailyAverage * 1.3m) * 8m, 0));
            recommendations.Add(new RecommendationDto(
                "rec_weekend_pacing",
                "WeekendPacing",
                weekend.WeekendVelocityRatio >= 2.5m ? "warning" : "info",
                "recommendations.weekendPacingTitle",
                $"Weekend spending velocity is {weekend.WeekendVelocityRatio:0.1}× higher than weekdays (Rp {weekend.WeekendDailyAverage:N0}/day vs Rp {weekend.WeekdayDailyAverage:N0}/day). Moderating weekend discretionary spend can save up to Rp {potentialMonthlySavings:N0} each month.",
                "recommendations.viewTransactions",
                "transactions",
                potentialMonthlySavings));
        }

        // 2. Savings Rate & Income Retention
        var income = analysis.Income;
        if (income.TotalIncome > 0 && income.RetentionRate < 20m)
        {
            var target20PctSavings = Math.Round(income.TotalIncome * 0.20m, 0);
            var currentSavings = Math.Max(0m, income.TotalIncome - analysis.Expense.TotalExpense);
            var gap = Math.Max(0m, target20PctSavings - currentSavings);

            recommendations.Add(new RecommendationDto(
                "rec_savings_retention",
                "SavingsRate",
                income.RetentionRate < 5m ? "critical" : "warning",
                "recommendations.savingsRateTitle",
                $"Your net income retention rate is currently {income.RetentionRate:0.1}%. Increasing savings to 20% by pruning non-essential expenses preserves an extra Rp {gap:N0}/month.",
                "recommendations.adjustBudget",
                "budget",
                gap));
        }

        // 3. Category Spending Surge
        var topSurge = analysis.Expense.CategoryMomentum
            .FirstOrDefault(c => c.GrowthPercent >= 30m && c.CurrentAmount >= 250_000m && c.TrailingAverageAmount > 0);

        if (topSurge != null)
        {
            var excess = Math.Max(0m, Math.Round(topSurge.CurrentAmount - topSurge.TrailingAverageAmount, 0));
            recommendations.Add(new RecommendationDto(
                "rec_category_surge",
                "CategorySurge",
                topSurge.GrowthPercent >= 50m ? "warning" : "info",
                "recommendations.categorySurgeTitle",
                $"Spending in an expanding category grew by +{topSurge.GrowthPercent:0.1}% over its 3-month trailing baseline. Re-anchoring this category to normal levels saves Rp {excess:N0}/month.",
                "recommendations.viewBudget",
                "budget",
                excess));
        }

        // 4. Cash Runway & Emergency Buffer Defense
        var avgMonthlyExpense = analysis.Expense.PreviousPeriodExpense > 0
            ? (analysis.Expense.TotalExpense + analysis.Expense.PreviousPeriodExpense) / 2m
            : analysis.Expense.TotalExpense;

        if (avgMonthlyExpense > 0)
        {
            var runwayMonths = Math.Round(liquidCash / avgMonthlyExpense, 1);
            if (runwayMonths < 3.0m)
            {
                var target3Months = avgMonthlyExpense * 3m;
                var deficit = Math.Max(0m, target3Months - liquidCash);
                var monthlyRamp = Math.Round(deficit / 6m, 0);

                recommendations.Add(new RecommendationDto(
                    "rec_cash_runway",
                    "CashRunway",
                    runwayMonths < 1.5m ? "critical" : "warning",
                    "recommendations.cashRunwayTitle",
                    $"Liquid emergency runway is currently {runwayMonths:0.1} months (Rp {liquidCash:N0}). Allocating Rp {monthlyRamp:N0}/month builds a safe 3-month safety buffer within 6 months.",
                    "recommendations.openAccounts",
                    "accounts",
                    null));
            }
        }

        // 5. Strategic Allocation Rebalancing (Master Plan)
        if (analysis.Expense.FixedVsVariable.VariablePercentage > 35m)
        {
            var suggestedTrim = Math.Round(analysis.Expense.TotalExpense * 0.05m, 0);
            recommendations.Add(new RecommendationDto(
                "rec_allocation_rebalance",
                "AllocationRebalance",
                "info",
                "recommendations.allocationRebalanceTitle",
                $"Variable discretionary spending represents {analysis.Expense.FixedVsVariable.VariablePercentage:0.1}% of total expenses. Realigning with the 50/40/10 Master Plan target unlocks Rp {suggestedTrim:N0}/month for long-term investments.",
                "recommendations.viewMasterPlan",
                "masterPlan",
                suggestedTrim));
        }

        return recommendations;
    }

    private static bool IsLiquid(string accountType) =>
        accountType.Equals("Cash", StringComparison.OrdinalIgnoreCase) ||
        accountType.Equals("Bank", StringComparison.OrdinalIgnoreCase) ||
        accountType.Equals("EWallet", StringComparison.OrdinalIgnoreCase) ||
        accountType.Equals("E-Wallet", StringComparison.OrdinalIgnoreCase) ||
        accountType.Equals("Savings", StringComparison.OrdinalIgnoreCase);
}
