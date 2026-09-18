namespace Tameru.Application.Abstractions;

public sealed record ChatMessagePrompt(
    string Role,
    string Content,
    string? Name = null,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    string? ToolCallId = null);

public sealed record ChatToolCall(
    string Id,
    string FunctionName,
    string FunctionArgumentsJson);

public sealed record ChatToolDefinition(
    string Name,
    string Description,
    object ParametersSchema);

public sealed record ChatCompletionResult(
    string? Content,
    IReadOnlyList<ChatToolCall>? ToolCalls = null,
    string? FinishReason = null);

public sealed record AiProviderConfig(
    string? Provider = null,
    string? BaseUrl = null,
    string? ApiKey = null,
    string? Model = null);

public interface IChatCompletionService
{
    bool IsConfigured { get; }

    Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<ChatMessagePrompt> messages,
        IReadOnlyList<ChatToolDefinition>? tools = null,
        AiProviderConfig? providerConfig = null,
        CancellationToken ct = default);
}
