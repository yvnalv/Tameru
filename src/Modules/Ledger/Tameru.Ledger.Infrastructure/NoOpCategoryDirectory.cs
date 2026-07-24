using Tameru.Modules.Contracts.Budgeting;

namespace Tameru.Ledger.Infrastructure;

/// <summary>
/// Permissive default <see cref="ICategoryDirectory"/> so the Ledger module can run without the
/// Budgeting module — it accepts any category id (active, flow "Any"). Registered via <c>TryAdd</c>
/// so Budgeting's real directory replaces it when present.
/// </summary>
internal sealed class NoOpCategoryDirectory : ICategoryDirectory
{
    public Task<CategoryRef?> GetAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult<CategoryRef?>(new CategoryRef(categoryId, "Category", "Any", IsActive: true));
}
