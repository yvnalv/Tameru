namespace Tameru.Modules.Contracts.Accounts;

/// <summary>
/// Derived account balances exposed by the Accounts module for read-model consumers (Reporting).
/// The balance is computed as <c>opening + net movement</c> from the ledger (ADR-0006); it is never
/// stored. Provided by Accounts so no other module reconstructs the balance formula or touches the
/// Accounts tables directly (docs/ARCHITECTURE.md).
/// </summary>
public interface IAccountBalanceDirectory
{
    /// <summary>
    /// Every account with its current derived balance. When <paramref name="activeOnly"/> is
    /// <c>true</c> (net-worth default, BR-023) inactive accounts are excluded.
    /// </summary>
    Task<IReadOnlyList<AccountBalance>> GetBalancesAsync(
        bool activeOnly = true, CancellationToken cancellationToken = default);
}

/// <summary>An account with its derived balance for reporting.</summary>
/// <param name="Id">Account id.</param>
/// <param name="Name">Account name (a user rename is shown verbatim).</param>
/// <param name="GroupName">Owning group's name, or <c>null</c> when ungrouped.</param>
/// <param name="Type">Account type (Cash / Bank / …) as a string for i18n mapping.</param>
/// <param name="CurrencyCode">The account's currency (IDR is functional).</param>
/// <param name="Balance">Derived balance: opening + net ledger movement.</param>
/// <param name="IsActive">Whether the account is active.</param>
public sealed record AccountBalance(
    Guid Id,
    string Name,
    string? GroupName,
    string Type,
    string CurrencyCode,
    decimal Balance,
    bool IsActive);
