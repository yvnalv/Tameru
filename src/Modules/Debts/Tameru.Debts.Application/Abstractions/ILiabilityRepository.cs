using Tameru.Application.Abstractions;
using Tameru.Debts.Domain;

namespace Tameru.Debts.Application.Abstractions;

public interface ILiabilityRepository
{
    Task<IReadOnlyList<Liability>> ListAsync(string? status = null, string? type = null, CancellationToken ct = default);
    Task<Liability?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Liability liability, CancellationToken ct = default);
    void Remove(Liability liability);
    Task<decimal> GetTotalRemainingActiveAsync(CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
    Task AddPaymentAsync(LiabilityPayment payment, CancellationToken ct = default);
    void RemovePayment(LiabilityPayment payment);
}

public interface IDebtsUnitOfWork : IUnitOfWork
{
}
