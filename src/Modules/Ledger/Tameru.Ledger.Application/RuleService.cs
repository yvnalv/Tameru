using System.Text.RegularExpressions;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Application;

public sealed class RuleService
{
    private readonly ICategorizationRuleRepository _rules;
    private readonly IRuleAuditLogRepository? _auditLogs;
    private readonly ILedgerUnitOfWork _unitOfWork;

    public static readonly IReadOnlyList<RuleDto> StandardTemplates = new List<RuleDto>
    {
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110001"),
            "Grab & Gojek Rides",
            "Gojek|Grab",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Transport - Ride Hailing",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "transport,ride,daily"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110002"),
            "Daily Coffee & Cafes",
            "Starbucks|Fore Coffee|Kopi Kenangan|Janji Jiwa|Tomoro Coffee",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Coffee & Beverage",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "food,coffee,daily"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110003"),
            "Supermarket & Groceries",
            "Indomaret|Alfamart|Superindo|Hypermart|Grand Lucky",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Groceries",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "groceries,food,essentials"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110004"),
            "E-Commerce & Online Shopping",
            "Tokopedia|Shopee|TikTok Shop|Blibli|Lazada",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Online Shopping",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "shopping,ecommerce"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110005"),
            "Monthly Salary Inflow",
            "Salary|Payroll|Gaji",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            50, true, 1000000, null, null, "Income", "Monthly Salary",
            GroupId: null, ScheduleExpression: "DOM:24-28", IsTemplate: true, Tags: "income,salary,payday"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110006"),
            "Digital Subscriptions",
            "Netflix|Spotify|YouTube|iCloud|ChatGPT|Google Storage",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Subscription Service",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "bills,subscriptions,entertainment"),
        new(
            Guid.Parse("11111111-1111-1111-1111-111111110007"),
            "Utilities & Internet Bills",
            "PLN|PDAM|BPJS|Telkom|Indihome|Biznet|MyRepublic",
            "Payee",
            "Regex",
            null, null, null, "Cleared",
            100, true, null, null, null, "Expense", "Utilities & Bills",
            GroupId: null, ScheduleExpression: null, IsTemplate: true, Tags: "bills,utilities,essentials"),
    };

    public RuleService(
        ICategorizationRuleRepository rules,
        ILedgerUnitOfWork unitOfWork,
        IRuleAuditLogRepository? auditLogs = null)
    {
        _rules = rules;
        _unitOfWork = unitOfWork;
        _auditLogs = auditLogs;
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
            status,
            request.MinAmount,
            request.MaxAmount,
            request.AccountId,
            request.TransactionType,
            request.ReplaceTitle,
            request.GroupId,
            request.ScheduleExpression,
            request.IsTemplate,
            request.Tags);

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
            status,
            request.MinAmount,
            request.MaxAmount,
            request.AccountId,
            request.TransactionType,
            request.ReplaceTitle,
            request.GroupId,
            request.ScheduleExpression,
            request.IsTemplate,
            request.Tags);

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

    public async Task<CategorizationRule?> FindMatchingRuleAsync(
        string payee,
        string? description,
        decimal? amount = null,
        Guid? accountId = null,
        string? type = null,
        DateTimeOffset? timestamp = null,
        CancellationToken ct = default)
    {
        var activeRules = await _rules.ListAsync(activeOnly: true, ct);
        var eligibleRules = activeRules
            .Where(r => !r.IsTemplate && r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToList();

        // Group-aware matching:
        // If a rule belongs to a GroupId, ALL active rules in that GroupId must match.
        // The first matching rule (or group) in priority order wins.
        var groups = eligibleRules
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupId))
            .GroupBy(r => r.GroupId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var evaluatedGroups = new Dictionary<string, bool>();

        foreach (var rule in eligibleRules)
        {
            if (string.IsNullOrWhiteSpace(rule.GroupId))
            {
                if (rule.Matches(payee, description, amount, accountId, type, timestamp))
                {
                    return rule;
                }
            }
            else
            {
                var groupId = rule.GroupId;
                if (!evaluatedGroups.TryGetValue(groupId, out var groupMatches))
                {
                    var groupRules = groups[groupId];
                    groupMatches = groupRules.All(r => r.Matches(payee, description, amount, accountId, type, timestamp));
                    evaluatedGroups[groupId] = groupMatches;
                }

                if (groupMatches)
                {
                    return rule;
                }
            }
        }

        return null;
    }

    public async Task<Result<DryRunResultDto>> DryRunAsync(DryRunRuleRequest request, CancellationToken ct = default)
    {
        var activeRules = await _rules.ListAsync(activeOnly: true, ct);
        var eligibleRules = activeRules
            .Where(r => !r.IsTemplate)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToList();

        string payee = request.Payee ?? request.Text ?? string.Empty;
        string? description = request.Description ?? request.Text;
        decimal? amount = request.Amount;
        Guid? accountId = request.AccountId;
        string? type = request.TransactionType;
        var now = DateTimeOffset.UtcNow;

        var groups = eligibleRules
            .Where(r => !string.IsNullOrWhiteSpace(r.GroupId))
            .GroupBy(r => r.GroupId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var evaluatedGroups = new Dictionary<string, bool>();

        var allEvaluations = new List<DryRunMatchDto>();
        DryRunMatchDto? winningMatch = null;

        foreach (var rule in eligibleRules)
        {
            bool matches;
            string reason;

            if (string.IsNullOrWhiteSpace(rule.GroupId))
            {
                matches = rule.Matches(payee, description, amount, accountId, type, now);
                reason = matches
                    ? $"Matched {rule.MatchField} with {rule.MatchOperator} \"{rule.Pattern}\""
                    : $"Did not match pattern or criteria";
            }
            else
            {
                var groupId = rule.GroupId;
                if (!evaluatedGroups.TryGetValue(groupId, out var groupMatches))
                {
                    var groupRules = groups[groupId];
                    groupMatches = groupRules.All(r => r.Matches(payee, description, amount, accountId, type, now));
                    evaluatedGroups[groupId] = groupMatches;
                }

                matches = groupMatches && rule.Matches(payee, description, amount, accountId, type, now);
                reason = matches
                    ? $"Matched as part of AND group '{rule.GroupId}'"
                    : (groupMatches ? "Rule in group passed, but group evaluation failed" : $"AND group '{rule.GroupId}' conditions not all satisfied");
            }

            var dto = new DryRunMatchDto(
                Rule: Map(rule),
                Matched: matches,
                MatchedReason: reason,
                ProjectedCategoryId: matches ? rule.TargetCategoryId : null,
                ProjectedBudgetCategoryId: matches ? rule.TargetBudgetCategoryId : null,
                ProjectedSubCategoryId: matches ? rule.TargetSubCategoryId : null,
                ProjectedStatus: matches ? rule.TargetStatus?.ToString() : null,
                ProjectedTitle: matches && !string.IsNullOrWhiteSpace(rule.ReplaceTitle) ? rule.ReplaceTitle : null);

            allEvaluations.Add(dto);

            if (matches && winningMatch is null)
            {
                winningMatch = dto;
            }
        }

        return Result.Success(new DryRunResultDto(
            HasMatch: winningMatch is not null,
            WinningMatch: winningMatch,
            AllEvaluations: allEvaluations));
    }

    public async Task<Result<IReadOnlyList<RuleAuditLogDto>>> ListAuditLogAsync(Guid? ruleId = null, int limit = 50, CancellationToken ct = default)
    {
        if (_auditLogs is null)
        {
            return Result.Success<IReadOnlyList<RuleAuditLogDto>>(Array.Empty<RuleAuditLogDto>());
        }

        var logs = await _auditLogs.ListAsync(ruleId, limit, ct);
        IReadOnlyList<RuleAuditLogDto> items = logs.Select(l => new RuleAuditLogDto(
            l.Id,
            l.RuleId,
            l.RuleName,
            l.TransactionId,
            l.TransactionTitle,
            l.Amount,
            l.MatchedField,
            l.MatchedValue,
            l.WasApplied,
            l.Details,
            l.EvaluatedAt)).ToList();

        return Result.Success(items);
    }

    public async Task RecordAuditLogAsync(
        Guid ruleId,
        string ruleName,
        Guid? transactionId,
        string transactionTitle,
        decimal? amount,
        string matchedField,
        string matchedValue,
        bool wasApplied,
        string? details = null,
        CancellationToken ct = default)
    {
        if (_auditLogs is null) return;

        var log = RuleAuditLog.Record(
            ruleId,
            ruleName,
            transactionId,
            transactionTitle,
            amount,
            matchedField,
            matchedValue,
            wasApplied,
            details);

        await _auditLogs.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<Result<IReadOnlyList<RuleDto>>> ListTemplatesAsync(CancellationToken ct = default)
    {
        var dbRules = await _rules.ListAsync(activeOnly: false, ct);
        var dbTemplates = dbRules.Where(r => r.IsTemplate).Select(Map).ToList();

        // Merge with standard templates, avoiding duplicates by Name
        var existingNames = new HashSet<string>(dbTemplates.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        var combined = new List<RuleDto>(dbTemplates);

        foreach (var standard in StandardTemplates)
        {
            if (!existingNames.Contains(standard.Name))
            {
                combined.Add(standard);
            }
        }

        IReadOnlyList<RuleDto> result = combined;
        return Result.Success(result);
    }

    public async Task<Result<RuleDto>> CreateFromTemplateAsync(
        Guid templateId,
        CreateFromTemplateRequest request,
        CancellationToken ct = default)
    {
        // 1. Look up in DB
        var dbRule = await _rules.GetByIdAsync(templateId, ct);
        RuleDto? baseTemplate = dbRule is not null ? Map(dbRule) : StandardTemplates.FirstOrDefault(t => t.Id == templateId);

        if (baseTemplate is null)
        {
            return LedgerErrors.RuleNotFound;
        }

        var createReq = new CreateRuleRequest(
            Name: request.Name ?? baseTemplate.Name,
            Pattern: request.Pattern ?? baseTemplate.Pattern,
            MatchField: baseTemplate.MatchField,
            MatchOperator: baseTemplate.MatchOperator,
            TargetCategoryId: request.TargetCategoryId ?? baseTemplate.TargetCategoryId,
            TargetBudgetCategoryId: request.TargetBudgetCategoryId ?? baseTemplate.TargetBudgetCategoryId,
            TargetSubCategoryId: request.TargetSubCategoryId ?? baseTemplate.TargetSubCategoryId,
            TargetStatus: baseTemplate.TargetStatus,
            Priority: baseTemplate.Priority,
            IsActive: true,
            MinAmount: request.MinAmount ?? baseTemplate.MinAmount,
            MaxAmount: request.MaxAmount ?? baseTemplate.MaxAmount,
            AccountId: request.AccountId ?? baseTemplate.AccountId,
            TransactionType: baseTemplate.TransactionType,
            ReplaceTitle: baseTemplate.ReplaceTitle,
            GroupId: request.GroupId ?? baseTemplate.GroupId,
            ScheduleExpression: request.ScheduleExpression ?? baseTemplate.ScheduleExpression,
            IsTemplate: false,
            Tags: request.Tags ?? baseTemplate.Tags);

        return await CreateAsync(createReq, ct);
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
        r.IsActive,
        r.MinAmount,
        r.MaxAmount,
        r.AccountId,
        r.TransactionType,
        r.ReplaceTitle,
        r.GroupId,
        r.ScheduleExpression,
        r.IsTemplate,
        r.Tags);
}
