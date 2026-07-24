namespace Tameru.Application.Abstractions;

/// <summary>
/// The authenticated owner for the current request. Tameru is single-user (ADR-0001), so this
/// resolves to the one account owner; it exists mainly to stamp audit fields.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The owner's user id, or <see cref="Guid.Empty"/> when unauthenticated (e.g. seeding).</summary>
    Guid UserId { get; }

    bool IsAuthenticated { get; }
}
