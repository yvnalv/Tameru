using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Application.Abstractions;

public interface IRecurringBillRepository
{
    Task<IReadOnlyList<RecurringBill>> ListAsync(bool? activeOnly = null, CancellationToken ct = default);
    Task<RecurringBill?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RecurringBill bill, CancellationToken ct = default);
    void Remove(RecurringBill bill);
}
