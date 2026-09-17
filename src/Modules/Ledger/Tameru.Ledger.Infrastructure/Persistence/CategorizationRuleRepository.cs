using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence;

internal sealed class CategorizationRuleRepository : ICategorizationRuleRepository
{
    private readonly LedgerDbContext _db;

    public CategorizationRuleRepository(LedgerDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategorizationRule>> ListAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _db.CategorizationRules.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.CategorizationRules.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(CategorizationRule rule, CancellationToken ct = default) =>
        await _db.CategorizationRules.AddAsync(rule, ct);

    public void Remove(CategorizationRule rule) => _db.CategorizationRules.Remove(rule);
}
