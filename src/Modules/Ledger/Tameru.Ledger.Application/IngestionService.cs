using System.Globalization;
using System.Text.RegularExpressions;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Budgeting;
using Tameru.Modules.Contracts.Ledger;
using Tameru.SharedKernel.Results;
using Tameru.SharedKernel.Time;

namespace Tameru.Ledger.Application;

public sealed class IngestionService
{
    private readonly LedgerService _ledgerService;
    private readonly RuleService _ruleService;
    private readonly IAccountDirectory _accounts;
    private readonly ICategoryDirectory? _categories;
    private readonly IClock _clock;

    public IngestionService(
        LedgerService ledgerService,
        RuleService ruleService,
        IAccountDirectory accounts,
        IClock clock,
        ICategoryDirectory? categories = null)
    {
        _ledgerService = ledgerService;
        _ruleService = ruleService;
        _accounts = accounts;
        _clock = clock;
        _categories = categories;
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

        var matchingRule = await _ruleService.FindMatchingRuleAsync(title, description, amount, accountId, type, _clock.UtcNow, ct);
        if (matchingRule is not null)
        {
            appliedRuleName = matchingRule.Name;
            if (!string.IsNullOrWhiteSpace(matchingRule.ReplaceTitle))
            {
                title = matchingRule.ReplaceTitle;
            }
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

        // 3. Smart Taxonomy Inference (Category & Budget Envelope)
        var taxonomy = _categories is not null ? await _categories.ListActiveTaxonomyAsync(ct) : Array.Empty<CategoryTaxonomyRef>();
        if (taxonomy.Count > 0)
        {
            if (categoryId.HasValue && !budgetCategoryId.HasValue)
            {
                var matchedCat = taxonomy.FirstOrDefault(c => c.Id == categoryId.Value);
                if (matchedCat?.ParentId.HasValue == true)
                {
                    budgetCategoryId = matchedCat.ParentId.Value;
                }
            }
            else if (!categoryId.HasValue)
            {
                var inferred = SmartInferCategory(title, request.Text, description, type, taxonomy);
                if (inferred.CategoryId.HasValue)
                {
                    categoryId = inferred.CategoryId.Value;
                    budgetCategoryId = inferred.BudgetCategoryId;
                }
                else if (inferred.BudgetCategoryId.HasValue && !budgetCategoryId.HasValue)
                {
                    budgetCategoryId = inferred.BudgetCategoryId.Value;
                }
            }
        }

        // 4. Create Transaction
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

        if (matchingRule is not null)
        {
            var matchedValue = matchingRule.MatchField == RuleMatchField.Description ? (description ?? string.Empty) : tx.Title;
            await _ruleService.RecordAuditLogAsync(
                matchingRule.Id,
                matchingRule.Name,
                tx.Id,
                tx.Title,
                amount,
                matchingRule.MatchField.ToString(),
                matchedValue,
                wasApplied: true,
                details: $"Categorized with rule '{matchingRule.Name}'",
                ct);
        }

        var catName = taxonomy.FirstOrDefault(c => c.Id == categoryId)?.Name;
        var budgetName = taxonomy.FirstOrDefault(c => c.Id == budgetCategoryId)?.Name;

        var formattedAmount = string.Format(CultureInfo.InvariantCulture, "Rp {0:N0}", amount).Replace(",", ".");
        var confirmation = $"✅ Logged: {formattedAmount} ({type}) · {tx.Title} · {matchedAccount.Name}";
        if (!string.IsNullOrWhiteSpace(catName) || !string.IsNullOrWhiteSpace(budgetName))
        {
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(catName)) metaParts.Add($"Category: {catName}");
            if (!string.IsNullOrWhiteSpace(budgetName)) metaParts.Add($"Budget: {budgetName}");
            metaParts.Add($"Status: {status}");
            confirmation += $"\n📁 {string.Join(" · ", metaParts)}";
        }

        return new IngestResultDto(tx, confirmation, appliedRuleName, matchedAccount.Name, catName, budgetName);
    }

    public async Task<Result<IngestResultDto>> UpdateAsync(UpdateIngestCommand command, CancellationToken ct = default)
    {
        var getResult = await _ledgerService.GetAsync(command.Id, ct);
        if (getResult.IsFailure)
        {
            return getResult.Error;
        }

        var existingTx = getResult.Value;

        var activeAccounts = await _accounts.ListActiveAccountsAsync(ct);
        var taxonomy = _categories is not null ? await _categories.ListActiveTaxonomyAsync(ct) : Array.Empty<CategoryTaxonomyRef>();

        var accountId = command.AccountId ?? existingTx.AccountId;
        var date = command.Date ?? existingTx.Date;
        var title = !string.IsNullOrWhiteSpace(command.Title) ? command.Title : existingTx.Title;
        var amount = command.Amount ?? existingTx.Amount;
        var status = !string.IsNullOrWhiteSpace(command.Status) ? command.Status : existingTx.Status;
        var description = command.Description ?? existingTx.Description;

        var categoryId = command.CategoryId ?? existingTx.CategoryId;
        var budgetCategoryId = command.BudgetCategoryId ?? existingTx.BudgetCategoryId;
        var subCategoryId = command.SubCategoryId ?? existingTx.SubCategoryId;

        // If category was changed but budget was not, auto-sync budget
        if (command.CategoryId.HasValue && !command.BudgetCategoryId.HasValue && taxonomy.Count > 0)
        {
            var cat = taxonomy.FirstOrDefault(c => c.Id == command.CategoryId.Value);
            if (cat?.ParentId.HasValue == true)
            {
                budgetCategoryId = cat.ParentId.Value;
            }
        }

        var updateReq = new UpdateTransactionRequest(
            Date: date,
            Title: title,
            Amount: amount,
            AccountId: accountId,
            ToAccountId: existingTx.ToAccountId,
            BudgetCategoryId: budgetCategoryId,
            CategoryId: categoryId,
            SubCategoryId: subCategoryId,
            Status: status,
            Description: description);

        var updateResult = await _ledgerService.UpdateAsync(command.Id, updateReq, ct);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        var updated = updateResult.Value;
        var matchedAcc = activeAccounts.FirstOrDefault(a => a.Id == accountId);
        var catName = taxonomy.FirstOrDefault(c => c.Id == updated.CategoryId)?.Name;
        var budgetName = taxonomy.FirstOrDefault(c => c.Id == updated.BudgetCategoryId)?.Name;

        var formattedAmount = string.Format(CultureInfo.InvariantCulture, "Rp {0:N0}", amount).Replace(",", ".");
        var confirmation = $"✏️ Updated: {formattedAmount} ({existingTx.Type}) · {updated.Title} · {matchedAcc?.Name ?? "Account"}";
        if (!string.IsNullOrWhiteSpace(catName) || !string.IsNullOrWhiteSpace(budgetName))
        {
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(catName)) metaParts.Add($"Category: {catName}");
            if (!string.IsNullOrWhiteSpace(budgetName)) metaParts.Add($"Budget: {budgetName}");
            metaParts.Add($"Status: {status}");
            confirmation += $"\n📁 {string.Join(" · ", metaParts)}";
        }

        return new IngestResultDto(updated, confirmation, null, matchedAcc?.Name, catName, budgetName);
    }

    public sealed record InferredTaxonomy(Guid? CategoryId, Guid? BudgetCategoryId);

    public static InferredTaxonomy SmartInferCategory(
        string title,
        string? text,
        string? description,
        string type,
        IReadOnlyList<CategoryTaxonomyRef> taxonomy)
    {
        if (taxonomy.Count == 0) return new InferredTaxonomy(null, null);

        var combined = $"{title} {text} {description}".ToLowerInvariant();

        // 1. Check if type is Income
        if (string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase))
        {
            var incomeBudget = taxonomy.FirstOrDefault(c => c.Level == "Budget" && c.Name.Equals("Income", StringComparison.OrdinalIgnoreCase));
            return new InferredTaxonomy(null, incomeBudget?.Id);
        }

        // Helper to find category and return with parent
        InferredTaxonomy MatchCategory(string categoryName, string? defaultBudgetName = null)
        {
            var cat = taxonomy.FirstOrDefault(c => c.Level == "Category" && c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                ?? taxonomy.FirstOrDefault(c => c.Level == "Category" && c.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase));

            if (cat is not null)
            {
                var parentBudget = cat.ParentId.HasValue
                    ? cat.ParentId.Value
                    : taxonomy.FirstOrDefault(c => c.Level == "Budget" && c.Name.Equals(defaultBudgetName, StringComparison.OrdinalIgnoreCase))?.Id;
                return new InferredTaxonomy(cat.Id, parentBudget);
            }

            if (!string.IsNullOrWhiteSpace(defaultBudgetName))
            {
                var budget = taxonomy.FirstOrDefault(c => c.Level == "Budget" && c.Name.Equals(defaultBudgetName, StringComparison.OrdinalIgnoreCase));
                return new InferredTaxonomy(null, budget?.Id);
            }

            return new InferredTaxonomy(null, null);
        }

        // 2. Food & Dining
        if (Regex.IsMatch(combined, @"\b(kopi|coffee|makan|restoran|restaurant|cafe|kafe|starbucks|fore|kenangan|janji jiwa|tomoro|indomaret|alfamart|superindo|hypermart|snack|lunch|dinner|breakfast|sarapan|food|bakso|mie|nasi|ayam|burger|pizza|mcd|kfc|hokben|kuliner|warung|dapur|cemilan|teh|boba|chatime|minuman|beverage)\b", RegexOptions.IgnoreCase))
        {
            return MatchCategory("Food", "Needs");
        }

        // 3. Transportation
        if (Regex.IsMatch(combined, @"\b(gojek|grab|gocar|goride|bensin|pertamina|shell|parkir|parking|tol|toll|transport|transportasi|transportation|ojol|taksi|taxi|mrt|krl|kereta|busway|transjakarta|flight|tiket|garuda|lion|kai|commuter)\b", RegexOptions.IgnoreCase))
        {
            return MatchCategory("Transportation", "Needs");
        }

        // 4. Internet & Utilities
        if (Regex.IsMatch(combined, @"\b(internet|wifi|indihome|biznet|myrepublic|firstmedia|telkomsel|indosat|xl|tri|smartfren|pulsa|kuota|pln|listrik|air|pdam|bpjs|tagihan|bill|utilities)\b", RegexOptions.IgnoreCase))
        {
            return MatchCategory("Internet", "Needs");
        }

        // 5. Entertainment
        if (Regex.IsMatch(combined, @"\b(netflix|spotify|youtube|disney|hbo|steam|playstation|game|gaming|bioskop|cinema|xxi|cgv|nonton|movie|concert|konser|liburan|holiday|hotel|entertainment|hiburan|rekreasi)\b", RegexOptions.IgnoreCase))
        {
            return MatchCategory("Entertainment", "Wants");
        }

        // 6. Personal & Shopping
        if (Regex.IsMatch(combined, @"\b(tokopedia|shopee|lazada|blibli|tiktok shop|belanja|shopping|baju|clothes|sepatu|shoes|skincare|makeup|salon|barbershop|potong rambut|personal|pribadi|hobi|hobby|gadget|fashion)\b", RegexOptions.IgnoreCase))
        {
            return MatchCategory("Personal", "Wants");
        }

        // 7. Investment & Savings
        if (Regex.IsMatch(combined, @"\b(tabungan|saving|investasi|investment|saham|stock|bibit|bareksa|ajaib|reksadana|emas|gold|antam|deposito|crypto|bitcoin)\b", RegexOptions.IgnoreCase))
        {
            if (combined.Contains("emas") || combined.Contains("gold") || combined.Contains("antam"))
            {
                var goldMatch = MatchCategory("Gold", "Investment");
                if (goldMatch.CategoryId.HasValue) return goldMatch;
            }
            return MatchCategory("Saving", "Investment");
        }

        // 8. Direct Category Name Matching against existing taxonomy
        foreach (var cat in taxonomy.Where(c => c.Level == "Category"))
        {
            if (combined.Contains(cat.Name.ToLowerInvariant()))
            {
                return new InferredTaxonomy(cat.Id, cat.ParentId);
            }
        }

        // 9. Default envelope fallback to Needs
        var defaultNeeds = taxonomy.FirstOrDefault(c => c.Level == "Budget" && c.Name.Equals("Needs", StringComparison.OrdinalIgnoreCase));
        return new InferredTaxonomy(null, defaultNeeds?.Id);
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
