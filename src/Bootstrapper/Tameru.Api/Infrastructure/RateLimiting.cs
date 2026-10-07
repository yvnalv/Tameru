using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Tameru.Web.Common.Contracts;

namespace Tameru.Api.Infrastructure;

/// <summary>
/// Request rate limiting for the credential endpoints (docs/SECURITY.md → "Rate-limit
/// <c>/auth/login</c> to slow brute force"). Tameru is single-user and internet-facing, so the
/// owner's email is effectively public and only the password resists guessing.
/// <para>
/// Limits are partitioned by client IP and driven by configuration — never hardcoded secrets or
/// magic numbers (CLAUDE.md → Deployment). Rejections use the standard failure envelope with the
/// documented <c>rate_limited</c> code (docs/ERROR_HANDLING.md).
/// </para>
/// </summary>
public static class RateLimiting
{
    /// <summary>Policy name applied to the credential endpoints.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Error code returned when a caller exceeds the window (docs/ERROR_HANDLING.md).</summary>
    public const string RateLimitedCode = "rate_limited";

    private const int DefaultPermitLimit = 10;
    private const int DefaultWindowSeconds = 60;

    public static IServiceCollection AddTameruRateLimiting(
        this IServiceCollection services,
        IConfiguration config)
    {
        var permitLimit = config.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? DefaultPermitLimit;
        var windowSeconds = config.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? DefaultWindowSeconds;

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        // Reject immediately rather than queueing: a queued login attempt is just a
                        // slower guess, and queueing would hold connections open under attack.
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    }));

            options.OnRejected = async (context, ct) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                await response.WriteAsJsonAsync(
                    ApiResponse.Fail(
                        "Too many requests. Please try again later.",
                        new ApiError { Code = RateLimitedCode }),
                    ct);
            };
        });

        return services;
    }

    /// <summary>
    /// Trusts <c>X-Forwarded-For</c> from the reverse proxy so the limiter partitions on the real
    /// client rather than the Nginx container. The API is never published directly — only Nginx can
    /// reach it — so the proxy list is left open and the hop count is configuration-driven
    /// (<c>RateLimiting:ForwardedHeaders:ForwardLimit</c>): 1 for the local Docker stack, 2 on a
    /// shared VPS where a second Nginx fronts the app.
    /// </summary>
    public static ForwardedHeadersOptions BuildForwardedHeadersOptions(IConfiguration config)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = config.GetValue<int?>("RateLimiting:ForwardedHeaders:ForwardLimit") ?? 1,
        };

        // Container IPs are assigned dynamically, so an allow-list of proxy addresses cannot be
        // maintained here; the network boundary is what keeps the API unreachable from outside.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        return options;
    }

    private static string PartitionKey(HttpContext httpContext)
    {
        var ip = httpContext.Connection.RemoteIpAddress;

        // A null remote address (in-memory test server, unix socket) must not collapse every caller
        // into one bucket silently — give it its own explicit partition.
        return ip is null ? "unknown" : ip.ToString();
    }
}
