using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Application.Abstractions;

public interface ICategorizationRuleRepository
{
    Task<IReadOnlyList<CategorizationRule>> ListAsync(bool activeOnly = false, CancellationToken cancellationToken = default);

    Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(CategorizationRule rule, CancellationToken cancellationToken = default);

    void Remove(CategorizationRule rule);
}
