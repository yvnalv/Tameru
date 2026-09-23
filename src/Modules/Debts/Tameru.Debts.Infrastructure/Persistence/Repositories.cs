using Microsoft.EntityFrameworkCore;
using Tameru.Debts.Application.Abstractions;
using Tameru.Debts.Domain;

namespace Tameru.Debts.Infrastructure.Persistence;

public sealed class LiabilityRepository : ILiabilityRepository
{
    private readonly DebtsDbContext _db;

    public LiabilityRepository(DebtsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Liability>> ListAsync(
        string? status = null,
        string? type = null,
        CancellationToken ct = default)
    {
        IQueryable<Liability> query = _db.Liabilities;

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(l => l.Type == type);
        }

        return await query
            .OrderBy(l => l.Status == LiabilityStatus.Active ? 0 : 1)
            .ThenBy(l => l.DueDate ?? DateOnly.MaxValue)
            .ThenBy(l => l.Title)
            .ToListAsync(ct);
    }

    public async Task<Liability?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Liabilities.FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task AddAsync(Liability liability, CancellationToken ct = default)
    {
        await _db.Liabilities.AddAsync(liability, ct);
    }

    public void Remove(Liability liability)
    {
        _db.Liabilities.Remove(liability);
    }

    public async Task<decimal> GetTotalRemainingActiveAsync(CancellationToken ct = default)
    {
        return await _db.Liabilities
            .Where(l => l.Status == LiabilityStatus.Active && l.Type != LiabilityType.Receivable)
            .SumAsync(l => l.RemainingBalance, ct);
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        return await _db.Liabilities
            .Where(l => l.Status == LiabilityStatus.Active && l.Type != LiabilityType.Receivable)
            .CountAsync(ct);
    }

    public async Task AddPaymentAsync(LiabilityPayment payment, CancellationToken ct = default)
    {
        await _db.LiabilityPayments.AddAsync(payment, ct);
    }

    public void RemovePayment(LiabilityPayment payment)
    {
        _db.LiabilityPayments.Remove(payment);
    }
}
