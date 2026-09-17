using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Application;

public sealed class RuleService
{
    private readonly ICategorizationRuleRepository _rules;
    private readonly ILedgerUnitOfWork _unitOfWork;

    public RuleService(ICategorizationRuleRepository rules, ILedgerUnitOfWork unitOfWork)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<RuleDto>>> ListAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var rules = await _rules.ListAsync(activeOnly, ct);
        IReadOnlyList<RuleDto> items = rules.Select(Map).ToList();
        return Result.Success(items);
    }

    public async Task<Result<RuleDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(id, ct);
        return rule is null ? LedgerErrors.RuleNotFound : Map(rule);
    }

    public async Task<Result<RuleDto>> CreateAsync(CreateRuleRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RuleMatchField>(request.MatchField, ignoreCase: true, out var matchField))
        {
            matchField = RuleMatchField.Payee;
        }

        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, ignoreCase: true, out var matchOp))
        {
            matchOp = RuleMatchOperator.Contains;
        }

        TransactionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.TargetStatus) &&
            Enum.TryParse<TransactionStatus>(request.TargetStatus, ignoreCase: true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var rule = CategorizationRule.Create(
            request.Name,
            request.Pattern,
            request.TargetCategoryId,
            request.TargetBudgetCategoryId,
            request.TargetSubCategoryId,
            matchField,
            matchOp,
            request.Priority,
            request.IsActive,
            status);

        await _rules.AddAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(rule);
    }

    public async Task<Result<RuleDto>> UpdateAsync(Guid id, UpdateRuleRequest request, CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(id, ct);
        if (rule is null) return LedgerErrors.RuleNotFound;

        if (!Enum.TryParse<RuleMatchField>(request.MatchField, ignoreCase: true, out var matchField))
        {
            matchField = RuleMatchField.Payee;
        }

        if (!Enum.TryParse<RuleMatchOperator>(request.MatchOperator, ignoreCase: true, out var matchOp))
        {
            matchOp = RuleMatchOperator.Contains;
        }

        TransactionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.TargetStatus) &&
            Enum.TryParse<TransactionStatus>(request.TargetStatus, ignoreCase: true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        rule.Update(
            request.Name,
            request.Pattern,
            request.TargetCategoryId,
            request.TargetBudgetCategoryId,
            request.TargetSubCategoryId,
            matchField,
            matchOp,
            request.Priority,
            request.IsActive,
            status);

        await _unitOfWork.SaveChangesAsync(ct);
        return Map(rule);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await _rules.GetByIdAsync(id, ct);
        if (rule is null) return LedgerErrors.RuleNotFound;

        _rules.Remove(rule);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<CategorizationRule?> FindMatchingRuleAsync(string payee, string? description, CancellationToken ct = default)
    {
        var activeRules = await _rules.ListAsync(activeOnly: true, ct);
        return activeRules.FirstOrDefault(r => r.Matches(payee, description));
    }

    private static RuleDto Map(CategorizationRule r) => new(
        r.Id,
        r.Name,
        r.Pattern,
        r.MatchField.ToString(),
        r.MatchOperator.ToString(),
        r.TargetCategoryId,
        r.TargetBudgetCategoryId,
        r.TargetSubCategoryId,
        r.TargetStatus?.ToString(),
        r.Priority,
        r.IsActive);
}
