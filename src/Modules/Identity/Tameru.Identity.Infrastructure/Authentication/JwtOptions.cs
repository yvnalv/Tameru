namespace Tameru.Identity.Infrastructure.Authentication;

/// <summary>JWT settings bound from the <c>Jwt</c> configuration section (docs/DEPLOYMENT.md).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "tameru";

    public string Audience { get; set; } = "tameru";

    public int AccessMinutes { get; set; } = 15;

    public int RefreshDays { get; set; } = 14;
}
