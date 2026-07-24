using Microsoft.EntityFrameworkCore;
using Tameru.Budgeting.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Budgeting;

namespace Tameru.Budgeting.Infrastructure;

/// <summary>
/// Implements the cross-module <see cref="ICategoryDirectory"/> so Ledger can validate a
/// transaction's category (exists / active / flow) without touching Budgeting tables directly.
/// </summary>
internal sealed class CategoryDirectory : ICategoryDirectory
{
    private readonly BudgetingDbContext _db;

    public CategoryDirectory(BudgetingDbContext db) => _db = db;

    public async Task<CategoryRef?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        await _db.Categories
            .Where(c => c.Id == categoryId)
            .Select(c => new CategoryRef(c.Id, c.Level.ToString(), c.Flow.ToString(), c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
}
