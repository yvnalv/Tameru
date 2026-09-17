using Tameru.SharedKernel.Domain;

namespace Tameru.Identity.Domain;

/// <summary>
/// The single account owner (ADR-0001). There is no roles/RBAC model; this entity exists to
/// authenticate the owner and to anchor audit stamping and personal preferences.
/// </summary>
public sealed class User : AuditableEntity
{
    public const string DefaultLocale = "en";
    public const int DefaultBudgetCycleStartDay = 1;

    private User()
    {
    }

    private User(Guid id, string email, string passwordHash, string displayName, string locale, int budgetCycleStartDay = DefaultBudgetCycleStartDay)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Locale = locale;
        BudgetCycleStartDay = budgetCycleStartDay;
    }

    /// <summary>Login identifier, stored normalized (trimmed, lower-case).</summary>
    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>UI language preference: <c>en</c> or <c>id</c>.</summary>
    public string Locale { get; private set; } = DefaultLocale;

    /// <summary>Starting day of month for the financial/budget cycle (1..28, e.g. 25 for payday).</summary>
    public int BudgetCycleStartDay { get; private set; } = DefaultBudgetCycleStartDay;

    public static User Create(string email, string passwordHash, string displayName, string? locale = null, int? budgetCycleStartDay = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainRuleException("email_required", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("password_required", "Password hash is required.");
        }

        var startDay = budgetCycleStartDay ?? DefaultBudgetCycleStartDay;
        if (startDay is < 1 or > 28)
        {
            throw new DomainRuleException("budget_cycle_start_day_invalid", "Cycle start day must be between 1 and 28.");
        }

        return new User(
            Guid.NewGuid(),
            Normalize(email),
            passwordHash,
            string.IsNullOrWhiteSpace(displayName) ? Normalize(email) : displayName.Trim(),
            NormalizeLocale(locale),
            startDay);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleException("password_required", "Password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    public void UpdateProfile(string? displayName, string? locale, int? budgetCycleStartDay = null)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(locale))
        {
            Locale = NormalizeLocale(locale);
        }

        if (budgetCycleStartDay.HasValue)
        {
            if (budgetCycleStartDay.Value is < 1 or > 28)
            {
                throw new DomainRuleException("budget_cycle_start_day_invalid", "Cycle start day must be between 1 and 28.");
            }
            BudgetCycleStartDay = budgetCycleStartDay.Value;
        }
    }

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static string NormalizeLocale(string? locale)
    {
        var value = (locale ?? DefaultLocale).Trim().ToLowerInvariant();
        return value is "en" or "id" ? value : DefaultLocale;
    }
}
