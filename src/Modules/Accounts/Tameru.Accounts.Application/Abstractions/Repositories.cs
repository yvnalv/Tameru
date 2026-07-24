using Tameru.Accounts.Domain;

namespace Tameru.Accounts.Application.Abstractions;

public interface IAccountRepository
{
    Task<IReadOnlyList<Account>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
}

public interface IAccountGroupRepository
{
    Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken cancellationToken = default);

    Task<AccountGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AccountGroup group, CancellationToken cancellationToken = default);
}

/// <summary>Module-scoped unit of work committing the Accounts <c>DbContext</c>.</summary>
public interface IAccountsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
