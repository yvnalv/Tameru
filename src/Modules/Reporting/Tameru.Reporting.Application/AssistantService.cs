using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Tameru.Application.Abstractions;
using Tameru.Modules.Contracts.Accounts;
using Tameru.Modules.Contracts.Identity;
using Tameru.Modules.Contracts.Ledger;
using Tameru.Reporting.Application.Contracts;
using Tameru.SharedKernel.Time;

namespace Tameru.Reporting.Application;

public sealed class AssistantService
{
    private static readonly ConcurrentDictionary<string, List<ChatMessagePrompt>> _conversations = new();

    private readonly IChatCompletionService _chatCompletion;
    private readonly ITransactionIngestor _transactionIngestor;
    private readonly DecisionService _decisionService;
    private readonly InsightsService _insightsService;
    private readonly ILedgerReportingQuery _ledgerQuery;
    private readonly IAccountBalanceDirectory _accountDirectory;
    private readonly IUserPreferences _userPreferences;
    private readonly IClock _clock;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        IChatCompletionService chatCompletion,
        ITransactionIngestor transactionIngestor,
        DecisionService decisionService,
        InsightsService insightsService,
        ILedgerReportingQuery ledgerQuery,
        IAccountBalanceDirectory accountDirectory,
        IUserPreferences userPreferences,
        IClock clock,
        ILogger<AssistantService> logger)
    {
        _chatCompletion = chatCompletion;
        _transactionIngestor = transactionIngestor;
        _decisionService = decisionService;
        _insightsService = insightsService;
        _ledgerQuery = ledgerQuery;
        _accountDirectory = accountDirectory;
        _userPreferences = userPreferences;
        _clock = clock;
        _logger = logger;
    }

    public void ClearConversation(string conversationId)
    {
        _conversations.TryRemove(conversationId, out _);
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? Guid.NewGuid().ToString("N")
            : request.ConversationId;

        var messageText = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return new ChatResponse("Please send a message or instruction.", conversationId);
        }

        var hasProviderConfig = request.Provider != null &&
            (!string.IsNullOrWhiteSpace(request.Provider.ApiKey) ||
             request.Provider.BaseUrl?.Contains("localhost", StringComparison.OrdinalIgnoreCase) == true ||
             request.Provider.BaseUrl?.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) == true);

        // Check if LLM is configured. If not, use intelligent local fallback
        if (!_chatCompletion.IsConfigured && !hasProviderConfig)
        {
            return await HandleLocalFallbackAsync(messageText, conversationId, ct);
        }

        var history = _conversations.GetOrAdd(conversationId, _ => new List<ChatMessagePrompt>());

        // If starting fresh session, add dynamic system prompt
        if (history.Count == 0)
        {
            var systemPrompt = await BuildSystemPromptAsync(ct);
            history.Add(new ChatMessagePrompt("system", systemPrompt));
        }

        history.Add(new ChatMessagePrompt("user", messageText));

        // Limit conversation history to last 15 messages to stay within token budgets
        if (history.Count > 16)
        {
            var systemMsg = history[0];
            var recent = history.Skip(history.Count - 15).ToList();
            history.Clear();
            history.Add(systemMsg);
            history.AddRange(recent);
        }

        var tools = GetToolDefinitions();

        var llmResult = await _chatCompletion.CompleteAsync(history, tools, request.Provider, ct);

        ChatAction? action = null;
        IReadOnlyList<InsightDto>? insights = null;

        // If LLM returned tool calls, execute them
        if (llmResult.ToolCalls is { Count: > 0 })
        {
            var toolCall = llmResult.ToolCalls[0];
            history.Add(new ChatMessagePrompt("assistant", llmResult.Content ?? string.Empty, ToolCalls: llmResult.ToolCalls));

            var toolOutcome = await ExecuteToolCallAsync(toolCall, ct);
            action = toolOutcome.Action;
            insights = toolOutcome.Insights;

            history.Add(new ChatMessagePrompt("tool", toolOutcome.ResultJson, ToolCallId: toolCall.Id));

            // Follow up completion with tool result to get final assistant response
            var followUp = await _chatCompletion.CompleteAsync(history, null, request.Provider, ct);
            var finalMessage = followUp.Content ?? toolOutcome.DefaultMessage;
            history.Add(new ChatMessagePrompt("assistant", finalMessage));

            return new ChatResponse(finalMessage, conversationId, action, insights);
        }

        var reply = llmResult.Content ?? "I've processed your request.";
        history.Add(new ChatMessagePrompt("assistant", reply));

        return new ChatResponse(reply, conversationId, action, insights);
    }

    private async Task<ChatResponse> HandleLocalFallbackAsync(string text, string conversationId, CancellationToken ct)
    {
        var lower = text.ToLowerInvariant();

        // 1. Transaction creation intent
        var hasAmount = Regex.IsMatch(text, @"\b(\d+([.,]\d+)?\s*(k|rb|jt|m|ribu|juta)?|\d{4,})\b", RegexOptions.IgnoreCase);
        var hasLogKeywords = Regex.IsMatch(text, @"\b(catat|tambah|log|add|beli|bayar|makan|kopi|spend|expense|income)\b", RegexOptions.IgnoreCase);

        if (hasAmount && (hasLogKeywords || text.Length < 60))
        {
            var outcome = await _transactionIngestor.IngestAsync(new IngestCommand(text), ct);
            if (outcome is not null)
            {
                var action = new ChatAction(
                    "transaction_created",
                    $"Logged {outcome.Title}: {FormatIdr(outcome.Amount)} ({outcome.Type})",
                    outcome);

                var reply = $"{outcome.ConfirmationMessage}\n\n*(Logged in offline mode. Set ASSISTANT_API_KEY for full conversational AI)*";
                return new ChatResponse(reply, conversationId, action);
            }
        }

        // 2. Safe to spend / Affordability query
        if (lower.Contains("aman") || lower.Contains("safe to spend") || lower.Contains("sisa") || lower.Contains("allowance") || lower.Contains("bisa beli") || lower.Contains("afford"))
        {
            // Extract amount if user is asking "can I afford 500k?"
            var match = Regex.Match(text, @"(\d+([.,]\d+)?)\s*(k|rb|jt|m|ribu|juta)?", RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", "."), CultureInfo.InvariantCulture, out var num))
            {
                var suffix = match.Groups[3].Value.ToLowerInvariant();
                var multiplier = suffix switch
                {
                    "k" or "rb" or "ribu" => 1000m,
                    "jt" or "juta" or "m" => 1000000m,
                    _ => 1m,
                };
                var purchaseAmount = num * multiplier;

                var simResult = await _decisionService.SimulatePurchaseAsync(new SimulatePurchaseRequest(purchaseAmount), ct);
                if (simResult.IsSuccess)
                {
                    var sim = simResult.Value;
                    var action = new ChatAction(
                        "simulation_run",
                        $"Simulated purchase of {FormatIdr(purchaseAmount)}: {sim.Verdict}",
                        sim);

                    var verdictText = sim.Verdict switch
                    {
                        "Safe" => "✅ **Safe to Purchase!** This expense is within your daily pacing budget.",
                        "Warning" => "⚠️ **Caution:** This purchase exceeds your monthly category allocation.",
                        _ => "🚨 **Risky:** This purchase will cut deeply into your emergency cash runway.",
                    };

                    var reply = $"{verdictText}\n- **Remaining Safe-to-Spend:** {FormatIdr(sim.NewSafeToSpend)}\n- **New Daily Allowance:** {FormatIdr(sim.NewDailyAllowance)}/day until next payday ({sim.DaysRemaining} days left).";
                    return new ChatResponse(reply, conversationId, action);
                }

                return new ChatResponse(simResult.Error.Message, conversationId);
            }

            var safe = await _decisionService.GetSafeToSpendAsync(ct);
            var safeReply = $"💰 **Safe-to-Spend Balance:** {FormatIdr(safe.SafeToSpend)}\n- **Daily Allowance:** {FormatIdr(safe.DailyAllowance)}/day\n- **Days Until Next Payday:** {safe.DaysRemaining} days ({safe.NextPayday:dd MMM yyyy})";
            return new ChatResponse(safeReply, conversationId);
        }

        // 3. Insights query
        if (lower.Contains("insight") || lower.Contains("wawasan") || lower.Contains("tips") || lower.Contains("saran"))
        {
            var insightList = await _insightsService.GetInsightsAsync(new InsightsRequest(MaxResults: 5), ct);
            var reply = insightList.Count > 0
                ? $"Found **{insightList.Count} proactive insights** based on your recent spending velocity and budgets."
                : "All systems are stable. No abnormal spending detected this cycle.";

            return new ChatResponse(reply, conversationId, null, insightList);
        }

        // 4. Default greeting / instructions
        return new ChatResponse(
            "Hello! I am your **Tameru Financial Copilot**.\n\n" +
            "You can quickly:\n" +
            "- **Log transactions:** `Kopi 35k gopay`, `Makan siang 45rb BCA`, `Gaji 15jt`\n" +
            "- **Check budgets:** `Safe to spend?`, `Berapa sisa uang aman?`\n" +
            "- **Test purchases:** `Can I afford 500k?`, `Aman beli keyboard 1.2jt?`\n" +
            "- **Get insights:** `Give me insights`, `Wawasan keuangan`\n\n" +
            "*Note: Currently in fast local mode. Configure `ASSISTANT_API_KEY` in environment for multi-turn conversational reasoning.*",
            conversationId);
    }

    private async Task<string> BuildSystemPromptAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var startDay = await _userPreferences.GetBudgetCycleStartDayAsync(ct);
        var safe = await _decisionService.GetSafeToSpendAsync(ct);

        return $@"You are Tameru Copilot, an intelligent, helpful, and concise personal finance assistant for the Tameru finance platform.
Current Date: {now:dddd, MMMM d, yyyy}.
Functional Currency: IDR (Indonesian Rupiah).
User Budget Cycle Starts on Day: {startDay}th of each month.
Current Safe-To-Spend Balance: Rp {safe.SafeToSpend:N0}.
Daily Pacing Allowance: Rp {safe.DailyAllowance:N0}/day.
Days Until Payday: {safe.DaysRemaining} days.

Guidelines:
1. Respond in the language the user speaks (English or Bahasa Indonesia). Understand colloquial Indonesian spending terms (e.g. '35k', '50rb', '1.5jt', 'gopay', 'bca', 'makan siang').
2. When the user tells you about an expense or income, ALWAYS call the 'create_transaction' tool.
3. When the user asks if they can afford something or whether a purchase is safe, ALWAYS call the 'simulate_purchase' tool.
4. When the user asks about their safe-to-spend allowance or remaining cash, call 'get_safe_to_spend'.
5. When the user asks for financial tips, anomalies, or insights, call 'get_insights'.
6. Keep answers concise, financial, and encouraging. Always format rupiah amounts clearly (e.g. Rp 35.000).";
    }

    private static IReadOnlyList<ChatToolDefinition> GetToolDefinitions()
    {
        return new List<ChatToolDefinition>
        {
            new(
                "create_transaction",
                "Records a transaction into the ledger with auto-categorization and natural text parsing.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The raw transaction text or phrase, e.g. 'kopi 35k gopay' or 'grocery 250000 bca category Food budget Needs'" },
                        amount = new { type = "number", description = "Optional explicit amount in IDR" },
                        title = new { type = "string", description = "Optional title or payee" },
                        type = new { type = "string", @enum = new[] { "Expense", "Income", "Transfer" }, description = "Transaction type" },
                        category = new { type = "string", description = "Optional category name e.g. 'Food', 'Transportation', 'Entertainment'" },
                        budget = new { type = "string", description = "Optional budget envelope name e.g. 'Needs', 'Wants', 'Investment'" },
                    },
                    required = new[] { "text" },
                }),
            new(
                "simulate_purchase",
                "Tests if a proposed purchase is safe given current liquidity, daily pacing, and upcoming payday.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        amount = new { type = "number", description = "Amount in IDR of the planned expense" },
                        itemName = new { type = "string", description = "Description of the purchase item" },
                    },
                    required = new[] { "amount" },
                }),
            new(
                "get_safe_to_spend",
                "Calculates current safe-to-spend uncommitted liquidity and daily pacing allowance until payday.",
                new
                {
                    type = "object",
                    properties = new { },
                }),
            new(
                "get_insights",
                "Retrieves proactive financial insights, spending velocity alerts, and anomalies.",
                new
                {
                    type = "object",
                    properties = new { },
                }),
        };
    }

    private async Task<ToolExecutionResult> ExecuteToolCallAsync(ChatToolCall toolCall, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(toolCall.FunctionArgumentsJson);
            var root = doc.RootElement;

            switch (toolCall.FunctionName)
            {
                case "create_transaction":
                {
                    var text = root.TryGetProperty("text", out var t) ? t.GetString() : null;
                    var amount = root.TryGetProperty("amount", out var a) && a.TryGetDecimal(out var amt) ? (decimal?)amt : null;
                    var title = root.TryGetProperty("title", out var ti) ? ti.GetString() : null;
                    var type = root.TryGetProperty("type", out var ty) ? ty.GetString() : null;
                    var category = root.TryGetProperty("category", out var cat) ? cat.GetString() : null;
                    var budget = root.TryGetProperty("budget", out var bg) ? bg.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(category) && text != null && !text.Contains(category, StringComparison.OrdinalIgnoreCase))
                    {
                        text = $"{text} category {category}";
                    }
                    if (!string.IsNullOrWhiteSpace(budget) && text != null && !text.Contains(budget, StringComparison.OrdinalIgnoreCase))
                    {
                        text = $"{text} budget {budget}";
                    }

                    var outcome = await _transactionIngestor.IngestAsync(new IngestCommand(text, amount, title, type), ct);
                    if (outcome is not null)
                    {
                        var action = new ChatAction(
                            "transaction_created",
                            $"Logged {outcome.Title}: {FormatIdr(outcome.Amount)} ({outcome.Type})",
                            outcome);

                        return new ToolExecutionResult(
                            ResultJson: JsonSerializer.Serialize(new {
                                success = true,
                                outcome.TransactionId,
                                outcome.Title,
                                outcome.Amount,
                                outcome.Type,
                                outcome.AccountName,
                                outcome.CategoryName,
                                outcome.BudgetName,
                                outcome.Status
                            }),
                            Action: action,
                            DefaultMessage: outcome.ConfirmationMessage);
                    }

                    return new ToolExecutionResult(
                        ResultJson: JsonSerializer.Serialize(new { success = false, error = "Failed to ingest transaction." }),
                        DefaultMessage: "Could not create the transaction. Please check the amount and try again.");
                }

                case "simulate_purchase":
                {
                    var amount = root.GetProperty("amount").GetDecimal();
                    var itemName = root.TryGetProperty("itemName", out var n) ? n.GetString() : null;

                    var simResult = await _decisionService.SimulatePurchaseAsync(new SimulatePurchaseRequest(amount, Description: itemName), ct);
                    if (simResult.IsSuccess)
                    {
                        var sim = simResult.Value;
                        var action = new ChatAction(
                            "simulation_run",
                            $"Simulated purchase of {FormatIdr(amount)}: {sim.Verdict}",
                            sim);

                        return new ToolExecutionResult(
                            ResultJson: JsonSerializer.Serialize(sim),
                            Action: action,
                            DefaultMessage: $"Purchase of {FormatIdr(amount)} evaluated as: **{sim.Verdict}**.");
                    }

                    return new ToolExecutionResult(
                        ResultJson: JsonSerializer.Serialize(new { error = simResult.Error.Message }),
                        DefaultMessage: simResult.Error.Message);
                }

                case "get_safe_to_spend":
                {
                    var safe = await _decisionService.GetSafeToSpendAsync(ct);
                    return new ToolExecutionResult(
                        ResultJson: JsonSerializer.Serialize(safe),
                        DefaultMessage: $"Safe to spend: {FormatIdr(safe.SafeToSpend)} ({FormatIdr(safe.DailyAllowance)}/day).");
                }

                case "get_insights":
                {
                    var insightList = await _insightsService.GetInsightsAsync(new InsightsRequest(MaxResults: 5), ct);
                    return new ToolExecutionResult(
                        ResultJson: JsonSerializer.Serialize(insightList),
                        Insights: insightList,
                        DefaultMessage: $"Surfaced {insightList.Count} financial insights.");
                }

                default:
                    return new ToolExecutionResult(
                        ResultJson: JsonSerializer.Serialize(new { error = $"Unknown function: {toolCall.FunctionName}" }),
                        DefaultMessage: "Unknown action.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing assistant tool call: {FunctionName}", toolCall.FunctionName);
            return new ToolExecutionResult(
                ResultJson: JsonSerializer.Serialize(new { error = ex.Message }),
                DefaultMessage: "Encountered an error performing the action.");
        }
    }

    public async Task<TestConnectionResponse> TestConnectionAsync(
        TestConnectionRequest request, CancellationToken ct = default)
    {
        try
        {
            var testPrompt = new List<ChatMessagePrompt>
            {
                new("user", "Respond with: Hello! Tameru AI connected successfully.")
            };

            var result = await _chatCompletion.CompleteAsync(testPrompt, null, request.Provider, ct);
            if (result.FinishReason is "error" or "no_api_key" or "exception")
            {
                return new TestConnectionResponse(
                    Success: false,
                    Message: result.Content ?? "Connection failed. Please verify provider URL, API key, and model name.",
                    Model: request.Provider?.Model);
            }

            return new TestConnectionResponse(
                Success: true,
                Message: result.Content?.Trim() ?? "Connection successful!",
                Model: request.Provider?.Model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test connection failed");
            return new TestConnectionResponse(
                Success: false,
                Message: $"Connection error: {ex.Message}",
                Model: request.Provider?.Model);
        }
    }

    private static string FormatIdr(decimal amount) =>
        string.Format(CultureInfo.InvariantCulture, "Rp {0:N0}", amount).Replace(",", ".");

    private sealed record ToolExecutionResult(
        string ResultJson,
        ChatAction? Action = null,
        IReadOnlyList<InsightDto>? Insights = null,
        string DefaultMessage = "Done");
}
