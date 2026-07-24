namespace Tameru.Application.Abstractions;

/// <summary>
/// Commits a use case's changes atomically. A single use case commits in one transaction
/// (docs/ARCHITECTURE.md → Request pipeline).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
