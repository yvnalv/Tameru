namespace Tameru.SharedKernel.Time;

/// <summary>
/// Abstracts "now" so domain and application logic stays deterministic and testable
/// (docs/TESTING.md: inject IClock; never use DateTime.Now in logic).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}

/// <summary>The real system clock (UTC). Registered in Infrastructure.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
