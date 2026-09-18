namespace Tameru.Reporting.Application.Contracts;

public sealed record ChatRequest(
    string Message,
    string? ConversationId = null);

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
