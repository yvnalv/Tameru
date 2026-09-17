namespace Tameru.Modules.Contracts.Ledger;

/// <summary>
/// Category spending reads exposed by the Ledger module and consumed by Budgeting to compute a
/// budget's <em>Actual</em> (BR-062). The ledger is the source of truth (ADR-0006); Budgeting never
/// queries ledger tables directly.
/// </summary>
public interface ICategorySpendQuery
{
    /// <summary>
    /// Sum of non-voided <b>expense</b> amounts in the given month, grouped by the category ids that
    /// classify each transaction (budget, category, and sub levels each accumulate their own id).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetExpenseTotalsByCategoryAsync(
        int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of non-voided <b>expense</b> amounts in the given date range (inclusive), grouped by category ids.
    /// Used when the budget cycle starts on a custom day (e.g. 25th of the month).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetExpenseTotalsByCategoryAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
