using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tameru.SharedKernel.Domain;
using Tameru.Web.Common.Contracts;

namespace Tameru.Web.Common.Middleware;

/// <summary>
/// Maps unhandled exceptions to the failure envelope with the right HTTP status
/// (docs/ERROR_HANDLING.md). Never leaks stack traces, SQL, or secrets to the client.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainRuleException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated: {Code}", ex.Code);
            await WriteAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Message,
                new ApiError { Code = ex.Code });
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                new ApiError { Code = "internal_error", TraceId = traceId });
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string message, ApiError error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse.Fail(message, error);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
