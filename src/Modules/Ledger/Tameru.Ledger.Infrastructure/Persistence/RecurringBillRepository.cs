using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence;

internal sealed class RecurringBillRepository : IRecurringBillRepository
{
    private readonly LedgerDbContext _db;

    public RecurringBillRepository(LedgerDbContext db) => _db = db;

    public async Task<IReadOnlyList<RecurringBill>> ListAsync(bool? activeOnly = null, CancellationToken ct = default)
    {
        var query = _db.RecurringBills.AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(b => b.IsActive == activeOnly.Value);
        }

        return await query
            .OrderBy(b => b.NextDueDate)
            .ThenBy(b => b.Title)
            .ToListAsync(ct);
    }

    public async Task<RecurringBill?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.RecurringBills.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task AddAsync(RecurringBill bill, CancellationToken ct = default) =>
        await _db.RecurringBills.AddAsync(bill, ct);

    public void Remove(RecurringBill bill) => _db.RecurringBills.Remove(bill);
}
