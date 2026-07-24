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
}
