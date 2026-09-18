namespace Tameru.Modules.Contracts.Budgeting;

/// <summary>
/// Cross-module contract providing budget status and recurring obligations for decision support.
/// </summary>
public interface IBudgetDecisionQuery
{
    /// <summary>
    /// Returns category budget allocations and spent actuals for the given cycle period.
    /// </summary>
    Task<IReadOnlyList<CategoryBudgetStatus>> GetCategoryBudgetsAsync(
        int year, int month, int startDay = 1, CancellationToken ct = default);

    /// <summary>
    /// Returns fixed/recurring obligations (such as Needs items from the master plan).
    /// </summary>
    Task<IReadOnlyList<FixedObligationRef>> GetFixedObligationsAsync(CancellationToken ct = default);
}

/// <summary>
/// Summary of a category's budget plan, actual expenditure, and remaining leftover.
/// </summary>
public sealed record CategoryBudgetStatus(
    Guid CategoryId,
    string CategoryName,
    decimal Plan,
    decimal Actual,
    decimal Leftover);

/// <summary>
/// A recurring fixed obligation (rent, utilities, insurance, subscriptions, etc.).
/// </summary>
public sealed record FixedObligationRef(
    Guid Id,
    string Name,
    decimal Amount,
    string SectionName,
    int? DueDay = null);
