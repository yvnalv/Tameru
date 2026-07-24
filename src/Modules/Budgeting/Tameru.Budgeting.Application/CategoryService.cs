using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Application.Contracts;
using Tameru.Budgeting.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Budgeting.Application;

/// <summary>Use cases for the Budget → Category → Sub taxonomy (BR-040..042).</summary>
public sealed class CategoryService
{
    private readonly ICategoryRepository _categories;
    private readonly IBudgetingUnitOfWork _unitOfWork;

    public CategoryService(ICategoryRepository categories, IBudgetingUnitOfWork unitOfWork)
    {
        _categories = categories;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> ListAsync(
        string? level, string? flow, Guid? parentId, bool includeInactive, CancellationToken ct = default)
    {
        CategoryLevel? levelFilter = null;
        if (!string.IsNullOrWhiteSpace(level))
        {
            if (!Enum.TryParse<CategoryLevel>(level, true, out var parsed))
            {
                return BudgetingErrors.InvalidLevel(level);
            }

            levelFilter = parsed;
        }

        CategoryFlow? flowFilter = null;
        if (!string.IsNullOrWhiteSpace(flow))
        {
            if (!Enum.TryParse<CategoryFlow>(flow, true, out var parsed))
            {
                return BudgetingErrors.InvalidFlow(flow);
            }

            flowFilter = parsed;
        }

        var items = await _categories.ListAsync(levelFilter, flowFilter, parentId, includeInactive, ct);
        return Result.Success<IReadOnlyList<CategoryDto>>(items.Select(Map).ToList());
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<CategoryLevel>(request.Level, true, out var level) || !Enum.IsDefined(level))
        {
            return BudgetingErrors.InvalidLevel(request.Level);
        }

        var flow = CategoryFlow.Any;
        if (!string.IsNullOrWhiteSpace(request.Flow)
            && (!Enum.TryParse(request.Flow, true, out flow) || !Enum.IsDefined(flow)))
        {
            return BudgetingErrors.InvalidFlow(request.Flow!);
        }

        if (level != CategoryLevel.Budget)
        {
            if (request.ParentId is not { } parentId)
            {
                return BudgetingErrors.InvalidParent("A Category or Sub must have a parent.");
            }

            var parent = await _categories.GetByIdAsync(parentId, ct);
            if (parent is null)
            {
                return BudgetingErrors.ParentNotFound;
            }

            var expectedParentLevel = level == CategoryLevel.Category ? CategoryLevel.Budget : CategoryLevel.Category;
            if (parent.Level != expectedParentLevel)
            {
                return BudgetingErrors.InvalidParent(
                    $"A {level} must have a {expectedParentLevel} parent, not a {parent.Level}.");
            }
        }

        var category = Category.Create(request.Name, level, request.ParentId, flow, false, request.SortOrder);
        await _categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<Result<CategoryDto>> UpdateAsync(
        Guid id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
        {
            return BudgetingErrors.CategoryNotFound;
        }

        var flow = category.Flow;
        if (!string.IsNullOrWhiteSpace(request.Flow)
            && (!Enum.TryParse(request.Flow, true, out flow) || !Enum.IsDefined(flow)))
        {
            return BudgetingErrors.InvalidFlow(request.Flow!);
        }

        category.Rename(request.Name, flow, request.SortOrder);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        if (category is null)
        {
            return BudgetingErrors.CategoryNotFound;
        }

        if (await _categories.HasChildrenAsync(id, ct))
        {
            return BudgetingErrors.CategoryInUse;
        }

        category.Deactivate(); // throws category_is_system for system categories (→ 409)
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static CategoryDto Map(Category c) => new(
        c.Id, c.Name, c.Level.ToString(), c.ParentId, c.Flow.ToString(), c.IsSystem, c.IsActive, c.SortOrder);
}
