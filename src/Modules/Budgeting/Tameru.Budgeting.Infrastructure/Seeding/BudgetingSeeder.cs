using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Domain;

namespace Tameru.Budgeting.Infrastructure.Seeding;

/// <summary>
/// Seeds the starter Budget → Category → Sub taxonomy and the Master Plan sections on first run
/// (idempotent). Mirrors the workbook's envelopes and the 40/50/10 split (ADR-0003, BR-081).
/// </summary>
public sealed class BudgetingSeeder
{
    private readonly ICategoryRepository _categories;
    private readonly IMasterPlanRepository _masterPlan;
    private readonly IBudgetingUnitOfWork _unitOfWork;

    public BudgetingSeeder(
        ICategoryRepository categories, IMasterPlanRepository masterPlan, IBudgetingUnitOfWork unitOfWork)
    {
        _categories = categories;
        _masterPlan = masterPlan;
        _unitOfWork = unitOfWork;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCategoriesAsync(cancellationToken);
        await SeedMasterPlanAsync(cancellationToken);
    }

    private async Task SeedCategoriesAsync(CancellationToken ct)
    {
        if (await _categories.AnyAsync(ct))
        {
            return;
        }

        // Budget-level envelopes.
        var income = Category.Create("Income", CategoryLevel.Budget, null, CategoryFlow.Income, isSystem: true, 0);
        var investment = Category.Create("Investment", CategoryLevel.Budget, null, CategoryFlow.Expense, false, 1);
        var needs = Category.Create("Needs", CategoryLevel.Budget, null, CategoryFlow.Expense, false, 2);
        var wants = Category.Create("Wants", CategoryLevel.Budget, null, CategoryFlow.Expense, false, 3);

        foreach (var budget in new[] { income, investment, needs, wants })
        {
            await _categories.AddAsync(budget, ct);
        }

        // A few Category-level children under each expense envelope.
        var children = new (string Name, Category Parent, int Order)[]
        {
            ("Saving", investment, 0),
            ("Gold", investment, 1),
            ("Food", needs, 0),
            ("Transportation", needs, 1),
            ("Internet", needs, 2),
            ("Personal", wants, 0),
            ("Entertainment", wants, 1),
        };

        foreach (var (name, parent, order) in children)
        {
            await _categories.AddAsync(
                Category.Create(name, CategoryLevel.Category, parent.Id, CategoryFlow.Any, false, order), ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task SeedMasterPlanAsync(CancellationToken ct)
    {
        if (await _masterPlan.AnySectionsAsync(ct))
        {
            return;
        }

        await _masterPlan.AddSectionAsync(MasterPlanSection.Create("Investment", 40m, 0), ct);
        await _masterPlan.AddSectionAsync(MasterPlanSection.Create("Needs", 50m, 1), ct);
        await _masterPlan.AddSectionAsync(MasterPlanSection.Create("Wants", 10m, 2), ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
