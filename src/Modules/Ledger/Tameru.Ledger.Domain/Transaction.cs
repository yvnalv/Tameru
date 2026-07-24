using Tameru.SharedKernel.Domain;
using Tameru.SharedKernel.ValueObjects;

namespace Tameru.Ledger.Domain;

/// <summary>
/// A single ledger entry — the source of truth for money movement (ADR-0002, ADR-0006). One of three
/// <see cref="TransactionType"/>s. Balances are derived from transactions; nothing here stores a
/// running balance. Rules enforced here: amount &gt; 0 (BR-001); a Transfer has a distinct target
/// account (BR-002); Income/Expense have no target (BR-003). Voiding is soft-delete (BR-007).
/// </summary>
public sealed class Transaction : AuditableEntity
{
    private Transaction()
    {
    }

    private Transaction(
        Guid id, TransactionType type, DateOnly date, string title, decimal amount, string currencyCode,
        Guid accountId, Guid? toAccountId, Guid? budgetCategoryId, Guid? categoryId, Guid? subCategoryId,
        TransactionStatus status, string? description)
        : base(id)
    {
        Type = type;
        Date = date;
        Title = title;
        Amount = amount;
        CurrencyCode = currencyCode;
        AccountId = accountId;
        ToAccountId = toAccountId;
        BudgetCategoryId = budgetCategoryId;
        CategoryId = categoryId;
        SubCategoryId = subCategoryId;
        Status = status;
        Description = description;
    }

    public TransactionType Type { get; private set; }

    public DateOnly Date { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = Money.FunctionalCurrency;

    /// <summary>Source account (Expense/Transfer) or destination account (Income).</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Destination account for a Transfer; null otherwise.</summary>
    public Guid? ToAccountId { get; private set; }

    public Guid? BudgetCategoryId { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid? SubCategoryId { get; private set; }

    public TransactionStatus Status { get; private set; }

    public string? Description { get; private set; }

    public static Transaction Income(
        DateOnly date, string title, decimal amount, Guid accountId,
        TransactionStatus status = TransactionStatus.Uncleared,
        Guid? budgetCategoryId = null, Guid? categoryId = null, Guid? subCategoryId = null,
        string? currencyCode = null, string? description = null) =>
        CreateFlow(TransactionType.Income, date, title, amount, accountId, null,
            budgetCategoryId, categoryId, subCategoryId, status, currencyCode, description);

    public static Transaction Expense(
        DateOnly date, string title, decimal amount, Guid accountId,
        TransactionStatus status = TransactionStatus.Uncleared,
        Guid? budgetCategoryId = null, Guid? categoryId = null, Guid? subCategoryId = null,
        string? currencyCode = null, string? description = null) =>
        CreateFlow(TransactionType.Expense, date, title, amount, accountId, null,
            budgetCategoryId, categoryId, subCategoryId, status, currencyCode, description);

    public static Transaction Transfer(
        DateOnly date, string title, decimal amount, Guid accountId, Guid toAccountId,
        TransactionStatus status = TransactionStatus.Uncleared,
        string? currencyCode = null, string? description = null)
    {
        if (toAccountId == Guid.Empty)
        {
            throw new DomainRuleException("transfer_target_required", "A transfer requires a target account.");
        }

        if (toAccountId == accountId)
        {
            throw new DomainRuleException("transfer_same_account",
                "A transfer's source and destination accounts must differ.");
        }

        return CreateFlow(TransactionType.Transfer, date, title, amount, accountId, toAccountId,
            null, null, null, status, currencyCode, description);
    }

    public void UpdateCommon(DateOnly date, string title, decimal amount, TransactionStatus status,
        string? description)
    {
        Guard(title, amount);
        Date = date;
        Title = title.Trim();
        Amount = amount;
        Status = status;
        Description = description;
    }

    public void ReassignAccount(Guid accountId) => AccountId = accountId;

    public void ReassignTransfer(Guid accountId, Guid toAccountId)
    {
        if (toAccountId == accountId)
        {
            throw new DomainRuleException("transfer_same_account",
                "A transfer's source and destination accounts must differ.");
        }

        AccountId = accountId;
        ToAccountId = toAccountId;
    }

    public void ReassignCategories(Guid? budgetCategoryId, Guid? categoryId, Guid? subCategoryId)
    {
        BudgetCategoryId = budgetCategoryId;
        CategoryId = categoryId;
        SubCategoryId = subCategoryId;
    }

    public void Clear() => Status = TransactionStatus.Cleared;

    public void Unclear() => Status = TransactionStatus.Uncleared;

    /// <summary>Signed effect on <see cref="AccountId"/>: +income, −expense, −transfer-out.</summary>
    public decimal SignedAmountForSource() => Type switch
    {
        TransactionType.Income => Amount,
        TransactionType.Expense => -Amount,
        TransactionType.Transfer => -Amount,
        _ => 0m,
    };

    private static Transaction CreateFlow(
        TransactionType type, DateOnly date, string title, decimal amount, Guid accountId, Guid? toAccountId,
        Guid? budgetCategoryId, Guid? categoryId, Guid? subCategoryId,
        TransactionStatus status, string? currencyCode, string? description)
    {
        Guard(title, amount);
        if (accountId == Guid.Empty)
        {
            throw new DomainRuleException("account_required", "An account is required.");
        }

        var currency = Money.Create(amount, currencyCode ?? Money.FunctionalCurrency).Currency;
        return new Transaction(Guid.NewGuid(), type, date, title.Trim(), amount, currency,
            accountId, toAccountId, budgetCategoryId, categoryId, subCategoryId, status, description);
    }

    private static void Guard(string title, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("title_required", "A transaction title is required.");
        }

        if (amount <= 0m)
        {
            throw new DomainRuleException("amount_not_positive", "Amount must be greater than zero.");
        }
    }
}
