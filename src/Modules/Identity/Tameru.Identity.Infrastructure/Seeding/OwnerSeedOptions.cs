namespace Tameru.Identity.Infrastructure.Seeding;

/// <summary>The owner account created on first run, bound from the <c>Seed:Owner</c> section.</summary>
public sealed class OwnerSeedOptions
{
    public const string SectionName = "Seed:Owner";

    public string Email { get; set; } = "owner@tameru.local";

    public string Password { get; set; } = "ChangeMe!123";

    public string DisplayName { get; set; } = "Owner";

    public string Locale { get; set; } = "en";
}
