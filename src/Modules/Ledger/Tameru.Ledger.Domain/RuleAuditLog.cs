using Tameru.SharedKernel.Domain;

namespace Tameru.Ledger.Domain;

/// <summary>
/// Audit trail recording whenever a categorization rule matches or is evaluated against a transaction.
/// </summary>
public sealed class RuleAuditLog : Entity
{
    private RuleAuditLog()
    {
    }

    public RuleAuditLog(
        Guid id,
        Guid ruleId,
        string ruleName,
        Guid? transactionId,
        string transactionTitle,
        decimal? amount,
        string matchedField,
        string matchedValue,
        bool wasApplied,
        string? details,
        DateTimeOffset evaluatedAt)
        : base(id)
    {
        RuleId = ruleId;
        RuleName = ruleName;
        TransactionId = transactionId;
        TransactionTitle = transactionTitle;
        Amount = amount;
        MatchedField = matchedField;
        MatchedValue = matchedValue;
        WasApplied = wasApplied;
        Details = details;
        EvaluatedAt = evaluatedAt;
    }

    public Guid RuleId { get; private set; }

    public string RuleName { get; private set; } = string.Empty;

    public Guid? TransactionId { get; private set; }

    public string TransactionTitle { get; private set; } = string.Empty;

    public decimal? Amount { get; private set; }

    public string MatchedField { get; private set; } = string.Empty;

    public string MatchedValue { get; private set; } = string.Empty;

    public bool WasApplied { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset EvaluatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static RuleAuditLog Record(
        Guid ruleId,
        string ruleName,
        Guid? transactionId,
        string transactionTitle,
        decimal? amount,
        string matchedField,
        string matchedValue,
        bool wasApplied,
        string? details = null)
    {
        return new RuleAuditLog(
            Guid.NewGuid(),
            ruleId,
            ruleName,
            transactionId,
            transactionTitle,
            amount,
            matchedField,
            matchedValue,
            wasApplied,
            details,
            DateTimeOffset.UtcNow);
    }
}
