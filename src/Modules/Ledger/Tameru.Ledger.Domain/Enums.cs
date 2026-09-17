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

/// <summary>Field targeted by a categorization rule.</summary>
public enum RuleMatchField
{
    Payee = 0,
    Description = 1,
}

/// <summary>Condition operator for matching a rule against transaction fields.</summary>
public enum RuleMatchOperator
{
    Contains = 0,
    Equals = 1,
    StartsWith = 2,
    Regex = 3,
}
