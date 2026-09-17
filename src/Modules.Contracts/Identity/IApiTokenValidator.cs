namespace Tameru.Modules.Contracts.Identity;

/// <summary>
/// Validates an API / Webhook ingestion token against the registered owner.
/// </summary>
public interface IApiTokenValidator
{
    Task<bool> ValidateAsync(string token, CancellationToken ct = default);
}
