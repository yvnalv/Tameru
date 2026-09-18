using Tameru.Application.Abstractions;

namespace Tameru.Reporting.Application.Contracts;

public sealed record ChatRequest(
    string Message,
    string? ConversationId = null,
    AiProviderConfig? Provider = null);

public sealed record TestConnectionRequest(
    AiProviderConfig? Provider = null);

public sealed record TestConnectionResponse(
    bool Success,
    string Message,
    string? Model = null);

public sealed record ChatAction(
    string Type,
    string Summary,
    object? Data = null);

public sealed record ChatResponse(
    string Message,
    string ConversationId,
    ChatAction? Action = null,
    IReadOnlyList<InsightDto>? Insights = null);

public sealed record ChatMessageItemDto(
    string Id,
    string Role,
    string Content,
    DateTimeOffset Timestamp,
    ChatAction? Action = null,
    IReadOnlyList<InsightDto>? Insights = null);
