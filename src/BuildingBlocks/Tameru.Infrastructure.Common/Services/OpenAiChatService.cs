using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tameru.Application.Abstractions;

namespace Tameru.Infrastructure.Common.Services;

public sealed class OpenAiChatService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiChatService> _logger;
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public OpenAiChatService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiChatService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["Assistant:ApiKey"]
            ?? configuration["ASSISTANT_API_KEY"]
            ?? configuration["OPENAI_API_KEY"];

        var baseUrl = configuration["Assistant:BaseUrl"]
            ?? configuration["ASSISTANT_BASE_URL"]
            ?? "https://api.openai.com/v1/";

        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }
        _baseUrl = baseUrl;

        _model = configuration["Assistant:Model"]
            ?? configuration["ASSISTANT_MODEL"]
            ?? "gpt-4o-mini";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessagePrompt> messages,
        IReadOnlyList<ChatToolDefinition>? tools = null,
        AiProviderConfig? providerConfig = null,
        CancellationToken ct = default)
    {
        var activeApiKey = !string.IsNullOrWhiteSpace(providerConfig?.ApiKey)
            ? providerConfig.ApiKey.Trim()
            : _apiKey;

        var rawBaseUrl = !string.IsNullOrWhiteSpace(providerConfig?.BaseUrl)
            ? providerConfig.BaseUrl.Trim()
            : _baseUrl;

        if (!rawBaseUrl.EndsWith('/'))
        {
            rawBaseUrl += "/";
        }

        var activeModel = !string.IsNullOrWhiteSpace(providerConfig?.Model)
            ? providerConfig.Model.Trim()
            : _model;

        var isLocal = rawBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || rawBaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(activeApiKey) && !isLocal)
        {
            _logger.LogWarning("OpenAiChatService called but no Assistant:ApiKey / ASSISTANT_API_KEY is configured.");
            return new ChatCompletionResult(
                Content: "Assistant is currently in local offline mode. To enable conversational AI with OpenAI, Groq, OpenRouter, or Ollama, configure your AI Provider in Settings.",
                ToolCalls: null,
                FinishReason: "no_api_key");
        }

        try
        {
            var requestBody = new JsonObject
            {
                ["model"] = activeModel,
                ["temperature"] = 0.2,
            };

            var messagesArray = new JsonArray();
            foreach (var msg in messages)
            {
                var msgObj = new JsonObject
                {
                    ["role"] = msg.Role,
                };

                if (!string.IsNullOrEmpty(msg.Content))
                {
                    msgObj["content"] = msg.Content;
                }

                if (!string.IsNullOrEmpty(msg.Name))
                {
                    msgObj["name"] = msg.Name;
                }

                if (!string.IsNullOrEmpty(msg.ToolCallId))
                {
                    msgObj["tool_call_id"] = msg.ToolCallId;
                }

                if (msg.ToolCalls is { Count: > 0 })
                {
                    var tcArray = new JsonArray();
                    foreach (var tc in msg.ToolCalls)
                    {
                        tcArray.Add(new JsonObject
                        {
                            ["id"] = tc.Id,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = tc.FunctionName,
                                ["arguments"] = tc.FunctionArgumentsJson,
                            },
                        });
                    }
                    msgObj["tool_calls"] = tcArray;
                }

                messagesArray.Add(msgObj);
            }
            requestBody["messages"] = messagesArray;

            if (tools is { Count: > 0 })
            {
                var toolsArray = new JsonArray();
                foreach (var tool in tools)
                {
                    var schemaNode = tool.ParametersSchema is string schemaJson
                        ? JsonNode.Parse(schemaJson)
                        : JsonSerializer.SerializeToNode(tool.ParametersSchema);

                    toolsArray.Add(new JsonObject
                    {
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = tool.Name,
                            ["description"] = tool.Description,
                            ["parameters"] = schemaNode,
                        },
                    });
                }
                requestBody["tools"] = toolsArray;
            }

            var requestUri = new Uri(new Uri(rawBaseUrl), "chat/completions");
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json"),
            };

            if (!string.IsNullOrWhiteSpace(activeApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", activeApiKey);
            }

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("OpenAI chat completion failed with status {StatusCode}: {Error}", response.StatusCode, errContent);
                return new ChatCompletionResult(
                    Content: $"Sorry, the AI service encountered an error ({response.StatusCode}). Please check your API key and provider settings.",
                    FinishReason: "error");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);

            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                return new ChatCompletionResult(Content: "No response was generated.", FinishReason: "empty");
            }

            var firstChoice = choices[0];
            var messageElem = firstChoice.GetProperty("message");
            var finishReason = firstChoice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;

            string? content = messageElem.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                ? c.GetString()
                : null;

            List<ChatToolCall>? toolCalls = null;
            if (messageElem.TryGetProperty("tool_calls", out var tcs) && tcs.ValueKind == JsonValueKind.Array)
            {
                toolCalls = new List<ChatToolCall>();
                foreach (var tc in tcs.EnumerateArray())
                {
                    var id = tc.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                    var fn = tc.GetProperty("function");
                    var fnName = fn.GetProperty("name").GetString() ?? string.Empty;
                    var fnArgs = fn.GetProperty("arguments").GetString() ?? "{}";
                    toolCalls.Add(new ChatToolCall(id, fnName, fnArgs));
                }
            }

            return new ChatCompletionResult(content, toolCalls, finishReason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in OpenAiChatService.CompleteAsync");
            return new ChatCompletionResult(
                Content: "Sorry, I had trouble communicating with the AI service. Please verify your connection or API key.",
                FinishReason: "exception");
        }
    }
}
