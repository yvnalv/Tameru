using Tameru.Identity.Domain;

namespace Tameru.Identity.Application.Abstractions;

/// <summary>Persistence for <see cref="User"/> (owned by the Identity module).</summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

/// <summary>Persistence for <see cref="RefreshToken"/>.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
}

/// <summary>
/// Module-scoped unit of work (commits the Identity <c>DbContext</c>). Kept module-specific to avoid
/// a single ambiguous global <c>IUnitOfWork</c> across modules (docs/ARCHITECTURE.md).
/// </summary>
public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
