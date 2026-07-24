namespace Tameru.Ledger.Domain;

/// <summary>The kind of cashflow a transaction represents (single-entry model, ADR-0002).</summary>
public enum TransactionType
{
    /// <summary>Increases an account's balance.</summary>
    Income = 0,

    /// <summary>Decreases an account's balance.</summary>
    Expense = 1,

    /// <summary>Moves an amount from one account to another.</summary>
    Transfer = 2,
}

/// <summary>Reconciliation marker; does not affect the derived balance (BR-009).</summary>
public enum TransactionStatus
{
    Uncleared = 0,
    Cleared = 1,
}
