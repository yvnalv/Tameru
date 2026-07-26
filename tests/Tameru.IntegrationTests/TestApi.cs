using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace Tameru.IntegrationTests;

// The response envelope (docs/API_SPEC.md) and minimal projections of the DTOs the tests assert on.
public sealed record Envelope<T>(bool Success, T? Data, string? Message, ApiError? Error);
public sealed record ApiError(string Code);

public sealed record AuthData(string AccessToken, string RefreshToken, UserInfo User);
public sealed record UserInfo(Guid Id, string Email, string DisplayName);
public sealed record AccountData(Guid Id, string Name, decimal OpeningBalance, decimal Balance);
public sealed record CategoryData(Guid Id, string Name, string Level, string Flow);
public sealed record TransactionData(Guid Id, string Type, decimal Amount, string Status);
public sealed record BudgetLineData(Guid CategoryId, decimal Plan, decimal Actual, decimal Leftover);
public sealed record BudgetPeriodData(
    Guid Id, int Year, int Month, IReadOnlyList<BudgetLineData> Lines,
    decimal TotalPlan, decimal TotalActual, decimal TotalLeftover);

/// <summary>Thin HTTP helper that unwraps the API envelope and manages the bearer token.</summary>
public sealed class TestApi
{
    public HttpClient Http { get; }

    public TestApi(HttpClient http) => Http = http;

    public async Task LoginAsync(string email, string password)
    {
        var auth = await PostAsync<AuthData>("/api/v1/auth/login", new { email, password });
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    public async Task<T> PostAsync<T>(string url, object body)
    {
        var response = await Http.PostAsJsonAsync(url, body);
        var env = await response.Content.ReadFromJsonAsync<Envelope<T>>();
        env.Should().NotBeNull();
        env!.Success.Should().BeTrue($"POST {url} should succeed but got: {env.Message} ({env.Error?.Code})");
        return env.Data!;
    }

    public async Task<T> GetAsync<T>(string url)
    {
        var env = await Http.GetFromJsonAsync<Envelope<T>>(url);
        env.Should().NotBeNull();
        env!.Success.Should().BeTrue($"GET {url} should succeed but got: {env.Message} ({env.Error?.Code})");
        return env.Data!;
    }

    public async Task PutAsync(string url, object body)
    {
        var response = await Http.PutAsJsonAsync(url, body);
        var env = await response.Content.ReadFromJsonAsync<Envelope<object>>();
        env!.Success.Should().BeTrue($"PUT {url} should succeed but got: {env.Message} ({env.Error?.Code})");
    }

    /// <summary>POST expecting a failure; returns the HTTP status and the stable error code.</summary>
    public async Task<(HttpStatusCode Status, string? Code)> PostExpectFailureAsync(string url, object body)
    {
        var response = await Http.PostAsJsonAsync(url, body);
        var env = await response.Content.ReadFromJsonAsync<Envelope<object>>();
        return (response.StatusCode, env?.Error?.Code);
    }
}
