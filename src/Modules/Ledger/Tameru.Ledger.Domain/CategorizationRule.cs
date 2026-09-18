using System.Text.RegularExpressions;
using Tameru.SharedKernel.Domain;

namespace Tameru.Ledger.Domain;

/// <summary>
/// A rule used by the ingestion and categorization engine to automatically match
/// incoming transactions (by payee, description, amount, account, or type) and assign
/// categories, budgets, status, or clean titles.
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
        TransactionStatus? targetStatus,
        decimal? minAmount,
        decimal? maxAmount,
        Guid? accountId,
        string? transactionType,
        string? replaceTitle,
        string? groupId = null,
        string? scheduleExpression = null,
        bool isTemplate = false,
        string? tags = null)
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
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        AccountId = accountId;
        TransactionType = transactionType;
        ReplaceTitle = replaceTitle;
        GroupId = groupId;
        ScheduleExpression = scheduleExpression;
        IsTemplate = isTemplate;
        Tags = tags;
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

    public decimal? MinAmount { get; private set; }

    public decimal? MaxAmount { get; private set; }

    public Guid? AccountId { get; private set; }

    public string? TransactionType { get; private set; }

    public string? ReplaceTitle { get; private set; }

    public string? GroupId { get; private set; }

    public string? ScheduleExpression { get; private set; }

    public bool IsTemplate { get; private set; }

    public string? Tags { get; private set; }

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
        TransactionStatus? targetStatus = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        Guid? accountId = null,
        string? transactionType = null,
        string? replaceTitle = null,
        string? groupId = null,
        string? scheduleExpression = null,
        bool isTemplate = false,
        string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("rule_name_required", "Rule name is required.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainRuleException("rule_pattern_required", "Pattern is required.");
        }

        if (targetCategoryId is null && targetBudgetCategoryId is null && targetStatus is null && string.IsNullOrWhiteSpace(replaceTitle))
        {
            throw new DomainRuleException("rule_action_required", "At least one target action (category, budget, status, or title replacement) must be specified.");
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
            targetStatus,
            minAmount,
            maxAmount,
            accountId,
            transactionType?.Trim(),
            replaceTitle?.Trim(),
            groupId?.Trim(),
            scheduleExpression?.Trim(),
            isTemplate,
            tags?.Trim());
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
        TransactionStatus? targetStatus,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        Guid? accountId = null,
        string? transactionType = null,
        string? replaceTitle = null,
        string? groupId = null,
        string? scheduleExpression = null,
        bool isTemplate = false,
        string? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("rule_name_required", "Rule name is required.");
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new DomainRuleException("rule_pattern_required", "Pattern is required.");
        }

        if (targetCategoryId is null && targetBudgetCategoryId is null && targetStatus is null && string.IsNullOrWhiteSpace(replaceTitle))
        {
            throw new DomainRuleException("rule_action_required", "At least one target action (category, budget, status, or title replacement) must be specified.");
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
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        AccountId = accountId;
        TransactionType = transactionType?.Trim();
        ReplaceTitle = replaceTitle?.Trim();
        GroupId = groupId?.Trim();
        ScheduleExpression = scheduleExpression?.Trim();
        IsTemplate = isTemplate;
        Tags = tags?.Trim();
    }

    public void ToggleActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool Matches(
        string? payee,
        string? description,
        decimal? amount = null,
        Guid? accountId = null,
        string? type = null,
        DateTimeOffset? timestamp = null)
    {
        if (!IsActive || IsTemplate) return false;

        if (!IsScheduleActive(timestamp ?? DateTimeOffset.UtcNow)) return false;

        // Account filter constraint
        if (AccountId.HasValue && accountId.HasValue && AccountId.Value != accountId.Value)
        {
            return false;
        }

        // Transaction type constraint
        if (!string.IsNullOrWhiteSpace(TransactionType) && !string.IsNullOrWhiteSpace(type)
            && !string.Equals(TransactionType, type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Amount thresholds constraint
        if (amount.HasValue)
        {
            if (MinAmount.HasValue && amount.Value < MinAmount.Value) return false;
            if (MaxAmount.HasValue && amount.Value > MaxAmount.Value) return false;
        }

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

    public bool IsScheduleActive(DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(ScheduleExpression)) return true;

        var expr = ScheduleExpression.Trim();
        if (expr.Equals("WEEKDAYS", StringComparison.OrdinalIgnoreCase))
        {
            return now.DayOfWeek != DayOfWeek.Saturday && now.DayOfWeek != DayOfWeek.Sunday;
        }
        if (expr.Equals("WEEKENDS", StringComparison.OrdinalIgnoreCase))
        {
            return now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday;
        }

        if (expr.StartsWith("DOM:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = expr[4..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int day = now.Day;
            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out var start) && int.TryParse(range[1], out var end))
                    {
                        if (day >= start && day <= end) return true;
                    }
                }
                else if (int.TryParse(part, out var singleDay))
                {
                    if (day == singleDay) return true;
                }
            }
            return false;
        }

        if (expr.StartsWith("DOW:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = expr[4..].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var currentDow = now.DayOfWeek.ToString();
            foreach (var part in parts)
            {
                if (currentDow.StartsWith(part, StringComparison.OrdinalIgnoreCase)) return true;
                if (int.TryParse(part, out var dowNum) && (int)now.DayOfWeek == dowNum) return true;
            }
            return false;
        }

        return true;
    }
}
