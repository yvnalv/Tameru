namespace Tameru.Modules.Contracts.Ledger;

/// <summary>
/// Account-oriented reads exposed by the Ledger module and consumed by Accounts to derive balances
/// and to guard deactivation. The Ledger is the single source of truth for movements (ADR-0006).
/// Until the Ledger module ships, a no-op implementation returns zero movement / no usage.
/// </summary>
public interface ILedgerAccountQuery
{
    /// <summary>
    /// Net movement per account up to and including <paramref name="asOf"/> (or all-time when null):
    /// <c>+income −expense −transfersOut +transfersIn</c>, excluding voided transactions.
    /// Accounts adds each account's opening balance to get the current balance.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetNetMovementByAccountAsync(
        DateOnly? asOf = null, CancellationToken cancellationToken = default);

    /// <summary>Net movement for a single account (see <see cref="GetNetMovementByAccountAsync"/>).</summary>
    Task<decimal> GetNetMovementAsync(
        Guid accountId, DateOnly? asOf = null, CancellationToken cancellationToken = default);

    /// <summary>Whether any non-voided transaction references the account (deactivation guard, BR-021).</summary>
    Task<bool> HasTransactionsAsync(Guid accountId, CancellationToken cancellationToken = default);
}
