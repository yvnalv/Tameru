namespace Tameru.Web.Common.Contracts;

/// <summary>
/// The standard API response envelope (CLAUDE.md → API Standards, docs/API_SPEC.md).
/// Success: <c>{ success: true, data }</c>. Failure: <c>{ success: false, message, error }</c>.
/// </summary>
public sealed class ApiResponse<T>
{
    private ApiResponse(bool success, T? data, string? message, ApiError? error)
    {
        Success = success;
        Data = data;
        Message = message;
        Error = error;
    }

    public bool Success { get; }

    public T? Data { get; }

    public string? Message { get; }

    public ApiError? Error { get; }

    public static ApiResponse<T> Ok(T data) => new(true, data, null, null);

    public static ApiResponse<T> Fail(string message, ApiError error) => new(false, default, message, error);
}

/// <summary>Non-generic success helper for empty payloads.</summary>
public static class ApiResponse
{
    public static ApiResponse<object> Ok() => ApiResponse<object>.Ok(new { });

    public static ApiResponse<object> Fail(string message, ApiError error) =>
        ApiResponse<object>.Fail(message, error);
}

/// <summary>Machine-readable error detail carried in a failure envelope.</summary>
public sealed class ApiError
{
    public required string Code { get; init; }

    public IReadOnlyList<ApiFieldError>? Details { get; init; }

    public string? TraceId { get; init; }
}

/// <summary>A single field validation error.</summary>
public sealed class ApiFieldError
{
    public required string Field { get; init; }

    public required string Message { get; init; }
}
