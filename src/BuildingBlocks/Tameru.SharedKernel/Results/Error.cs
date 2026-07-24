namespace Tameru.SharedKernel.Results;

/// <summary>
/// A machine-readable error: a stable <see cref="Code"/> plus a human-readable
/// <see cref="Message"/>. Codes map to HTTP status in the API (docs/ERROR_HANDLING.md).
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("validation_error", message);

    public static Error NotFound(string message) => new("not_found", message);

    public static Error Conflict(string message) => new("conflict", message);

    public static Error Unprocessable(string code, string message) => new(code, message);
}
