namespace Tameru.Ledger.Application.Contracts;

/// <summary>A ledger transaction as returned to the client (enums as strings for i18n mapping).</summary>
public sealed record TransactionDto(
    Guid Id,
    string Type,
    DateOnly Date,
    string Title,
    decimal Amount,
    string CurrencyCode,
    Guid AccountId,
    Guid? ToAccountId,
    Guid? BudgetCategoryId,
    Guid? CategoryId,
    Guid? SubCategoryId,
    string Status,
    string? Description);

public sealed record CreateTransactionRequest(
    string Type,
    DateOnly Date,
    string Title,
    decimal Amount,
    Guid AccountId,
    Guid? ToAccountId,
    Guid? BudgetCategoryId,
    Guid? CategoryId,
    Guid? SubCategoryId,
    string? Status,
    string? CurrencyCode,
    string? Description);

/// <summary>Updates a transaction's mutable fields; the transaction <c>Type</c> is immutable.</summary>
public sealed record UpdateTransactionRequest(
    DateOnly Date,
    string Title,
    decimal Amount,
    Guid AccountId,
    Guid? ToAccountId,
    Guid? BudgetCategoryId,
    Guid? CategoryId,
    Guid? SubCategoryId,
    string? Status,
    string? Description);

/// <summary>List/filter criteria for transactions (docs/API_SPEC.md → Transactions).</summary>
public sealed record TransactionFilter(
    string? Type = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    string? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? Query = null,
    int Page = 1,
    int PageSize = 50);
