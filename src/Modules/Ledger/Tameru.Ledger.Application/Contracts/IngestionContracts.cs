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
    bool IsActive,
    decimal? MinAmount,
    decimal? MaxAmount,
    Guid? AccountId,
    string? TransactionType,
    string? ReplaceTitle,
    string? GroupId = null,
    string? ScheduleExpression = null,
    bool IsTemplate = false,
    string? Tags = null);

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
    bool IsActive = true,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    Guid? AccountId = null,
    string? TransactionType = null,
    string? ReplaceTitle = null,
    string? GroupId = null,
    string? ScheduleExpression = null,
    bool IsTemplate = false,
    string? Tags = null);

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
    bool IsActive,
    decimal? MinAmount,
    decimal? MaxAmount,
    Guid? AccountId,
    string? TransactionType,
    string? ReplaceTitle,
    string? GroupId = null,
    string? ScheduleExpression = null,
    bool IsTemplate = false,
    string? Tags = null);

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
    string? AppliedRuleName,
    string? AccountName = null,
    string? CategoryName = null,
    string? BudgetName = null);

public sealed record DryRunRuleRequest(
    string? Text = null,
    string? Payee = null,
    string? Description = null,
    decimal? Amount = null,
    Guid? AccountId = null,
    string? TransactionType = null);

public sealed record DryRunMatchDto(
    RuleDto Rule,
    bool Matched,
    string? MatchedReason,
    Guid? ProjectedCategoryId,
    Guid? ProjectedBudgetCategoryId,
    Guid? ProjectedSubCategoryId,
    string? ProjectedStatus,
    string? ProjectedTitle);

public sealed record DryRunResultDto(
    bool HasMatch,
    DryRunMatchDto? WinningMatch,
    IReadOnlyList<DryRunMatchDto> AllEvaluations);

public sealed record RuleAuditLogDto(
    Guid Id,
    Guid RuleId,
    string RuleName,
    Guid? TransactionId,
    string TransactionTitle,
    decimal? Amount,
    string MatchedField,
    string MatchedValue,
    bool WasApplied,
    string? Details,
    DateTimeOffset EvaluatedAt);

public sealed record CreateFromTemplateRequest(
    string? Name = null,
    string? Pattern = null,
    Guid? TargetCategoryId = null,
    Guid? TargetBudgetCategoryId = null,
    Guid? TargetSubCategoryId = null,
    Guid? AccountId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? GroupId = null,
    string? ScheduleExpression = null,
    string? Tags = null);
