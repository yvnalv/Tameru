using Tameru.SharedKernel.Results;

namespace Tameru.Budgeting.Application;

/// <summary>Stable Budgeting error codes (docs/ERROR_HANDLING.md, BUSINESS_RULES.md).</summary>
public static class BudgetingErrors
{
    public static readonly Error CategoryNotFound = Error.NotFound("Category not found.");

    public static readonly Error ParentNotFound = Error.NotFound("Parent category not found.");

    public static readonly Error PeriodNotFound = Error.NotFound("Budget period not found.");

    public static readonly Error SectionNotFound = Error.NotFound("Master plan section not found.");

    public static readonly Error ItemNotFound = Error.NotFound("Master plan item not found.");

    public static readonly Error PeriodExists =
        new("conflict", "A budget for this year and month already exists.");

    public static readonly Error CategoryInUse =
        new("category_in_use", "This category has child categories and cannot be deactivated.");

    public static Error InvalidLevel(string value) =>
        Error.Validation($"'{value}' is not a valid category level.");

    public static Error InvalidFlow(string value) =>
        Error.Validation($"'{value}' is not a valid category flow.");

    public static Error InvalidParent(string message) => Error.Unprocessable("category_invalid_parent", message);
}
