using System.Globalization;
using System.Text.RegularExpressions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.SharedKernel.Results;
using Tameru.SharedKernel.Time;

namespace Tameru.Ledger.Application;

public sealed class IngestionService
{
    private readonly LedgerService _ledgerService;
    private readonly RuleService _ruleService;
    private readonly IAccountDirectory _accounts;
    private readonly IClock _clock;

    public IngestionService(
        LedgerService ledgerService,
        RuleService ruleService,
        IAccountDirectory accounts,
        IClock clock)
    {
        _ledgerService = ledgerService;
        _ruleService = ruleService;
        _accounts = accounts;
        _clock = clock;
    }

    public async Task<Result<IngestResultDto>> IngestAsync(IngestTransactionRequest request, CancellationToken ct = default)
    {
        var activeAccounts = await _accounts.ListActiveAccountsAsync(ct);
        if (activeAccounts.Count == 0)
        {
            return Error.Validation("No active accounts found in system.");
        }

        decimal amount = request.Amount ?? 0;
        string title = request.Title ?? string.Empty;
        string type = request.Type ?? "Expense";
        Guid accountId = request.AccountId ?? Guid.Empty;
        DateOnly date = request.Date ?? DateOnly.FromDateTime(_clock.UtcNow.DateTime);
        string? description = request.Description;

        // 1. If natural text is provided, parse it
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            var parsed = ParseNaturalText(request.Text, activeAccounts);
            if (parsed.Amount > 0 && amount == 0) amount = parsed.Amount;
            if (!string.IsNullOrWhiteSpace(parsed.Title) && string.IsNullOrWhiteSpace(title)) title = parsed.Title;
            if (accountId == Guid.Empty && parsed.AccountId.HasValue) accountId = parsed.AccountId.Value;
            if (string.IsNullOrWhiteSpace(request.Type) && !string.IsNullOrWhiteSpace(parsed.Type)) type = parsed.Type;
        }

        if (amount <= 0)
        {
            return Error.Validation("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = type == "Income" ? "Income" : "Expense";
        }

        // Default to first account if still empty
        if (accountId == Guid.Empty)
        {
            var defaultAcc = activeAccounts.FirstOrDefault(a => a.Type == "Bank")
                ?? activeAccounts.FirstOrDefault(a => a.Type == "Cash")
                ?? activeAccounts[0];
            accountId = defaultAcc.Id;
        }

        var matchedAccount = activeAccounts.FirstOrDefault(a => a.Id == accountId) ?? activeAccounts[0];

        // 2. Evaluate Categorization Rules
        Guid? categoryId = request.CategoryId;
        Guid? budgetCategoryId = null;
        Guid? subCategoryId = null;
        string status = "Cleared";
        string? appliedRuleName = null;

        var matchingRule = await _ruleService.FindMatchingRuleAsync(title, description, ct);
        if (matchingRule is not null)
        {
            appliedRuleName = matchingRule.Name;
            if (categoryId is null && matchingRule.TargetCategoryId.HasValue)
            {
                categoryId = matchingRule.TargetCategoryId.Value;
            }
            budgetCategoryId = matchingRule.TargetBudgetCategoryId;
            subCategoryId = matchingRule.TargetSubCategoryId;
            if (matchingRule.TargetStatus.HasValue)
            {
                status = matchingRule.TargetStatus.Value.ToString();
            }
        }

        // 3. Create Transaction
        var createRequest = new CreateTransactionRequest(
            Type: type,
            Date: date,
            Title: title,
            Amount: amount,
            AccountId: accountId,
            ToAccountId: null,
            BudgetCategoryId: budgetCategoryId,
            CategoryId: categoryId,
            SubCategoryId: subCategoryId,
            Status: status,
            CurrencyCode: null,
            Description: description);

        var result = await _ledgerService.CreateAsync(createRequest, ct);
        if (result.IsFailure)
        {
            return result.Error;
        }

        var tx = result.Value;
        var formattedAmount = string.Format(CultureInfo.InvariantCulture, "Rp {0:N0}", amount).Replace(",", ".");
        var confirmation = $"✅ Logged: {formattedAmount} ({type}) · {tx.Title} · {matchedAccount.Name}";

        return new IngestResultDto(tx, confirmation, appliedRuleName);
    }

    public static ParsedTransaction ParseNaturalText(string rawText, IReadOnlyList<AccountRef> accounts)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ParsedTransaction(0, string.Empty, "Expense", null);
        }

        var text = rawText.Trim();

        // 1. Detect Type
        var type = "Expense";
        var incomeKeywords = @"\b(gaji|salary|income|bonus|cair|reimburse|dapat|terima|masuk)\b";
        if (Regex.IsMatch(text, incomeKeywords, RegexOptions.IgnoreCase))
        {
            type = "Income";
        }

        // 2. Detect Amount (e.g. 35k, 50rb, 1.5jt, 2.5m, 150000, 150.000, Rp 25.000)
        decimal amount = 0;
        var amountRegex = new Regex(@"(?:rp\.?\s*)?(\d+(?:[.,]\d+)?)\s*(jt|m|k|rb)?\b", RegexOptions.IgnoreCase);
        var match = amountRegex.Match(text);
        if (match.Success)
        {
            var rawNum = match.Groups[1].Value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(rawNum, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            {
                var unit = match.Groups[2].Value.ToLowerInvariant();
                amount = unit switch
                {
                    "k" or "rb" => num * 1000m,
                    "jt" or "m" => num * 1000000m,
                    _ => num,
                };
            }

            text = text.Remove(match.Index, match.Length);
        }

        // 3. Detect Account
        Guid? matchedAccountId = null;
        foreach (var acc in accounts)
        {
            var accPattern = @"\b" + Regex.Escape(acc.Name) + @"\b";
            if (Regex.IsMatch(text, accPattern, RegexOptions.IgnoreCase))
            {
                matchedAccountId = acc.Id;
                text = Regex.Replace(text, accPattern, "", RegexOptions.IgnoreCase);
                break;
            }
        }

        // 4. Clean up Title / Payee
        // Remove common Indonesian prepositions & ingestion verbs
        var fillers = @"\b(di|ke|via|pakai|menggunakan|dari|for|at|with|dapat|terima|masuk|cair)\b";
        text = Regex.Replace(text, fillers, "", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            text = type == "Income" ? "Income" : "Expense";
        }
        else
        {
            text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
        }

        return new ParsedTransaction(amount, text, type, matchedAccountId);
    }
}

public sealed record ParsedTransaction(decimal Amount, string Title, string Type, Guid? AccountId);
