using Tameru.Ledger.Domain;

namespace Tameru.Ledger.Application.Abstractions;

public interface IRuleAuditLogRepository
{
    Task AddAsync(RuleAuditLog log, CancellationToken ct = default);

    Task<IReadOnlyList<RuleAuditLog>> ListAsync(Guid? ruleId = null, int limit = 50, CancellationToken ct = default);
}
