using Tameru.SharedKernel.Domain;

namespace Tameru.Budgeting.Domain;

/// <summary>
/// A node in the three-level Budget → Category → Sub taxonomy (a single self-referencing table,
/// CLAUDE.md → Categories). A <see cref="CategoryLevel.Category"/>'s parent is a Budget; a
/// <see cref="CategoryLevel.Sub"/>'s parent is a Category; a Budget has no parent (BR-040). System
/// categories cannot be deleted (BR-041).
/// </summary>
public sealed class Category : AuditableEntity
{
    private Category()
    {
    }

    private Category(
        Guid id, string name, CategoryLevel level, Guid? parentId, CategoryFlow flow, bool isSystem, int sortOrder)
        : base(id)
    {
        Name = name;
        Level = level;
        ParentId = parentId;
        Flow = flow;
        IsSystem = isSystem;
        IsActive = true;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = string.Empty;

    public CategoryLevel Level { get; private set; }

    public Guid? ParentId { get; private set; }

    public CategoryFlow Flow { get; private set; }

    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public static Category Create(
        string name, CategoryLevel level, Guid? parentId, CategoryFlow flow = CategoryFlow.Any,
        bool isSystem = false, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("category_name_required", "Category name is required.");
        }

        if (level == CategoryLevel.Budget && parentId is not null)
        {
            throw new DomainRuleException("category_budget_no_parent", "A Budget-level category has no parent.");
        }

        if (level != CategoryLevel.Budget && parentId is null)
        {
            throw new DomainRuleException("category_parent_required",
                "A Category or Sub must have a parent.");
        }

        return new Category(Guid.NewGuid(), name.Trim(), level, parentId, flow, isSystem, sortOrder);
    }

    public void Rename(string name, CategoryFlow flow, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("category_name_required", "Category name is required.");
        }

        Name = name.Trim();
        Flow = flow;
        SortOrder = sortOrder;
    }

    public void Deactivate()
    {
        if (IsSystem)
        {
            throw new DomainRuleException("category_is_system", "System categories cannot be deactivated.");
        }

        IsActive = false;
    }

    public void Activate() => IsActive = true;

    /// <summary>Whether this category may classify a transaction of the given flow (BR-005).</summary>
    public bool AcceptsFlow(CategoryFlow transactionFlow) =>
        Flow == CategoryFlow.Any || Flow == transactionFlow;
}
