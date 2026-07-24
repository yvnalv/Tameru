namespace Tameru.Modules.Contracts.Accounts;

/// <summary>
/// Read-only account lookups exposed by the Accounts module for other modules (e.g. Ledger
/// validating that a transaction's account exists and is active). Provided by Accounts.
/// </summary>
public interface IAccountDirectory
{
    Task<bool> ExistsAndActiveAsync(Guid accountId, CancellationToken cancellationToken = default);

    /// <summary>The account's currency code, or <c>null</c> if the account does not exist.</summary>
    Task<string?> GetCurrencyAsync(Guid accountId, CancellationToken cancellationToken = default);
}
