namespace Tameru.Ledger.Application.Contracts;

public sealed record RuleDto(
    Guid Id,
    string Name,
    string Pattern,
    string MatchField,
    string MatchOperator,
    Guid? TargetCategoryId,
    Guid? TargetBudgetCategoryId,
    Guid? TargetSubCategoryId,
    string? TargetStatus,
    int Priority,
    bool IsActive);

public sealed record CreateRuleRequest(
    string Name,
    string Pattern,
    string MatchField = "Payee",
    string MatchOperator = "Contains",
    Guid? TargetCategoryId = null,
    Guid? TargetBudgetCategoryId = null,
    Guid? TargetSubCategoryId = null,
    string? TargetStatus = null,
    int Priority = 100,
    bool IsActive = true);

public sealed record UpdateRuleRequest(
    string Name,
    string Pattern,
    string MatchField,
    string MatchOperator,
    Guid? TargetCategoryId,
    Guid? TargetBudgetCategoryId,
    Guid? TargetSubCategoryId,
    string? TargetStatus,
    int Priority,
    bool IsActive);

public sealed record IngestTransactionRequest(
    string? Text = null,
    decimal? Amount = null,
    string? Title = null,
    string? Type = null,
    string? AccountName = null,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    DateOnly? Date = null,
    string? Description = null);

public sealed record IngestResultDto(
    TransactionDto Transaction,
    string FormattedConfirmation,
    string? AppliedRuleName);
