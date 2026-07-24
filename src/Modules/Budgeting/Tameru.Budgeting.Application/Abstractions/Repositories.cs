using Tameru.Budgeting.Domain;

namespace Tameru.Budgeting.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(
        CategoryLevel? level, CategoryFlow? flow, Guid? parentId, bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task<bool> HasChildrenAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);
}

public interface IBudgetRepository
{
    Task<BudgetPeriod?> GetPeriodAsync(int year, int month, CancellationToken cancellationToken = default);

    Task<BudgetPeriod?> GetPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetPeriod>> ListPeriodsAsync(int? year, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BudgetLine>> ListLinesAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task AddPeriodAsync(BudgetPeriod period, CancellationToken cancellationToken = default);

    Task AddLineAsync(BudgetLine line, CancellationToken cancellationToken = default);
}

public interface IMasterPlanRepository
{
    Task<IReadOnlyList<MasterPlanSection>> ListSectionsAsync(CancellationToken cancellationToken = default);

    Task<MasterPlanSection?> GetSectionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MasterPlanItem>> ListItemsAsync(CancellationToken cancellationToken = default);

    Task<MasterPlanItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnySectionsAsync(CancellationToken cancellationToken = default);

    Task AddSectionAsync(MasterPlanSection section, CancellationToken cancellationToken = default);

    Task AddItemAsync(MasterPlanItem item, CancellationToken cancellationToken = default);

    void RemoveItem(MasterPlanItem item);
}

/// <summary>Module-scoped unit of work committing the Budgeting <c>DbContext</c>.</summary>
public interface IBudgetingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
