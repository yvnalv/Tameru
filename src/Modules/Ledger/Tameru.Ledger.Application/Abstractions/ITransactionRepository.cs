using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Ledger.Application.Abstractions;

public interface ITransactionRepository
{
    Task<PagedResult<Transaction>> ListAsync(TransactionFilter filter, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Marks a transaction for removal; the context turns this into a soft-delete (void, BR-007).</summary>
    void Remove(Transaction transaction);
}

/// <summary>Module-scoped unit of work committing the Ledger <c>DbContext</c>.</summary>
public interface ILedgerUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
