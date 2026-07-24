namespace Tameru.Ledger.Domain;

/// <summary>
/// Pure derivation of account balances from transactions (ADR-0006, docs/DATABASE.md → Derived
/// balance). The single source of truth for how money moves; the Infrastructure query mirrors this
/// in SQL. Voided (soft-deleted) transactions must be excluded by the caller/query.
/// </summary>
public static class BalanceCalculator
{
    /// <summary>
    /// Net movement for <paramref name="accountId"/> over <paramref name="transactions"/>, optionally
    /// up to and including <paramref name="asOf"/>:
    /// <c>+income −expense −transfersOut +transfersIn</c>. Does not include the opening balance.
    /// </summary>
    public static decimal NetMovement(
        Guid accountId, IEnumerable<Transaction> transactions, DateOnly? asOf = null)
    {
        var total = 0m;
        foreach (var t in transactions)
        {
            if (asOf is { } cutoff && t.Date > cutoff)
            {
                continue;
            }

            if (t.AccountId == accountId)
            {
                total += t.SignedAmountForSource();
            }

            if (t.Type == TransactionType.Transfer && t.ToAccountId == accountId)
            {
                total += t.Amount;
            }
        }

        return total;
    }

    /// <summary>Current balance = opening balance + net movement (ADR-0006).</summary>
    public static decimal Balance(
        Guid accountId, decimal openingBalance, IEnumerable<Transaction> transactions, DateOnly? asOf = null) =>
        openingBalance + NetMovement(accountId, transactions, asOf);
}
