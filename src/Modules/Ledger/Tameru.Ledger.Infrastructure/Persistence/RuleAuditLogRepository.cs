using Microsoft.EntityFrameworkCore;
using Tameru.Ledger.Application.Abstractions;
using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Infrastructure.Persistence;

internal sealed class RuleAuditLogRepository : IRuleAuditLogRepository
{
    private readonly LedgerDbContext _db;

    public RuleAuditLogRepository(LedgerDbContext db) => _db = db;

    public async Task AddAsync(RuleAuditLog log, CancellationToken ct = default) =>
        await _db.RuleAuditLogs.AddAsync(log, ct);

    public async Task<IReadOnlyList<RuleAuditLog>> ListAsync(Guid? ruleId = null, int limit = 50, CancellationToken ct = default)
    {
        var query = _db.RuleAuditLogs.AsQueryable();

        if (ruleId.HasValue)
        {
            query = query.Where(l => l.RuleId == ruleId.Value);
        }

        return await query
            .OrderByDescending(l => l.EvaluatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}
