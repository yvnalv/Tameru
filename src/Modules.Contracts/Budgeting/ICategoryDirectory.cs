namespace Tameru.Modules.Contracts.Budgeting;

/// <summary>
/// Category lookups exposed by the Budgeting module for other modules (Ledger validates a
/// transaction's category exists, is active, and its flow matches the transaction type — BR-005/006).
/// Provided by Budgeting. A no-op default (accept anything) is used until Budgeting ships.
/// </summary>
public interface ICategoryDirectory
{
    Task<CategoryRef?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

/// <summary>A minimal projection of a category for cross-module validation.</summary>
/// <param name="Id">Category id.</param>
/// <param name="Level">Budget / Category / Sub.</param>
/// <param name="Flow">Income / Expense / Transfer / Any.</param>
/// <param name="IsActive">Whether the category is active.</param>
public sealed record CategoryRef(Guid Id, string Level, string Flow, bool IsActive);
