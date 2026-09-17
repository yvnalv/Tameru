using System.Text.RegularExpressions;
using Tameru.SharedKernel.Domain;

namespace Tameru.Ledger.Domain;

/// <summary>
/// A rule used by the ingestion and categorization engine to automatically match
/// incoming transactions (by payee or description) and assign categories, budgets, or status.
/// </summary>
public sealed class CategorizationRule : AuditableEntity
{
    private CategorizationRule()
    {
    }

    private CategorizationRule(
        Guid id,
        string name,
        int priority,
        bool isActive,
        RuleMatchField matchField,
        RuleMatchOperator matchOperator,
        string pattern,
        Guid? targetCategoryId,
        Guid? targetBudgetCategoryId,
        Guid? targetSubCategoryId,
        TransactionStatus? targetStatus)
        : base(id)
    {
        Name = name;
        Priority = priority;
        IsActive = isActive;
        MatchField = matchField;
        MatchOperator = matchOperator;
        Pattern = pattern;
        TargetCategoryId = targetCategoryId;
        TargetBudgetCategoryId = targetBudgetCategoryId;
        TargetSubCategoryId = targetSubCategoryId;
        TargetStatus = targetStatus;
    }

    public string Name { get; private set; } = string.Empty;

    public int Priority { get; private set; } = 100;

    public bool IsActive { get; private set; } = true;

    public RuleMatchField MatchField { get; private set; } = RuleMatchField.Payee;

    public RuleMatchOperator MatchOperator { get; private set; } = RuleMatchOperator.Contains;

    public string Pattern { get; private set; } = string.Empty;

    public Guid? TargetCategoryId { get; private set; }

    public Guid? TargetBudgetCategoryId { get; private set; }

    public Guid? TargetSubCategoryId { get; private set; }

    public TransactionStatus? TargetStatus { get; private set; }

    public static CategorizationRule Create(
        string name,
        string pattern,
        Guid? targetCategoryId = null,
        Guid? targetBudgetCategoryId = null,
        Guid? targetSubCategoryId = null,
        RuleMatchField matchField = RuleMatchField.Payee,
        RuleMatchOperator matchOperator = RuleMatchOperator.Contains,
        int priority = 100,
        bool isActive = true,
        TransactionStatus? targetStatus = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("rule_name_required", "Rule name is required.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainRuleException("rule_pattern_required", "Pattern is required.");
        }

        if (targetCategoryId is null && targetBudgetCategoryId is null && targetStatus is null)
        {
            throw new DomainRuleException("rule_action_required", "At least one target action (category, budget, or status) must be specified.");
        }

        return new CategorizationRule(
            Guid.NewGuid(),
            name.Trim(),
            priority,
            isActive,
            matchField,
            matchOperator,
            pattern.Trim(),
            targetCategoryId,
            targetBudgetCategoryId,
            targetSubCategoryId,
            targetStatus);
    }

    public void Update(
        string name,
        string pattern,
        Guid? targetCategoryId,
        Guid? targetBudgetCategoryId,
        Guid? targetSubCategoryId,
        RuleMatchField matchField,
        RuleMatchOperator matchOperator,
        int priority,
        bool isActive,
        TransactionStatus? targetStatus)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("rule_name_required", "Rule name is required.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainRuleException("rule_pattern_required", "Pattern is required.");
        }

        if (targetCategoryId is null && targetBudgetCategoryId is null && targetStatus is null)
        {
            throw new DomainRuleException("rule_action_required", "At least one target action (category, budget, or status) must be specified.");
        }

        Name = name.Trim();
        Pattern = pattern.Trim();
        TargetCategoryId = targetCategoryId;
        TargetBudgetCategoryId = targetBudgetCategoryId;
        TargetSubCategoryId = targetSubCategoryId;
        MatchField = matchField;
        MatchOperator = matchOperator;
        Priority = priority;
        IsActive = isActive;
        TargetStatus = targetStatus;
    }

    public void ToggleActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool Matches(string? payee, string? description)
    {
        if (!IsActive) return false;

        var text = MatchField switch
        {
            RuleMatchField.Description => description ?? string.Empty,
            _ => payee ?? string.Empty,
        };

        if (string.IsNullOrEmpty(text)) return false;

        return MatchOperator switch
        {
            RuleMatchOperator.Equals => string.Equals(text, Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.StartsWith => text.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchOperator.Regex => Regex.IsMatch(text, Pattern, RegexOptions.IgnoreCase),
            _ => text.Contains(Pattern, StringComparison.OrdinalIgnoreCase),
        };
    }
}
