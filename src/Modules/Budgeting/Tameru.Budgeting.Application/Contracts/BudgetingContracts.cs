namespace Tameru.Budgeting.Application.Contracts;

// --- Categories -------------------------------------------------------------
public sealed record CategoryDto(
    Guid Id, string Name, string Level, Guid? ParentId, string Flow, bool IsSystem, bool IsActive, int SortOrder);

public sealed record CreateCategoryRequest(
    string Name, string Level, Guid? ParentId, string? Flow, int SortOrder);

public sealed record UpdateCategoryRequest(string Name, string? Flow, int SortOrder);

// --- Budget -----------------------------------------------------------------
public sealed record BudgetLineDto(
    Guid CategoryId, string? CategoryName, decimal Plan, decimal Actual, decimal Leftover);

public sealed record BudgetPeriodDto(
    Guid Id, int Year, int Month, string? Note,
    IReadOnlyList<BudgetLineDto> Lines,
    decimal TotalPlan, decimal TotalActual, decimal TotalLeftover);

public sealed record BudgetPeriodSummaryDto(Guid Id, int Year, int Month, string? Note);

public sealed record CreateBudgetPeriodRequest(int Year, int Month, string? Note);

public sealed record BudgetLineInput(Guid CategoryId, decimal PlanAmount);

public sealed record UpsertBudgetLinesRequest(IReadOnlyList<BudgetLineInput> Lines);

// --- Master Plan ------------------------------------------------------------
public sealed record MasterPlanItemDto(
    Guid Id, Guid SectionId, string Name, decimal Price, int Frequency, decimal TotalBudget, int SortOrder);

public sealed record MasterPlanSectionDto(
    Guid Id, string Name, decimal TargetPercent, int SortOrder,
    IReadOnlyList<MasterPlanItemDto> Items, decimal Total);

public sealed record MasterPlanDto(IReadOnlyList<MasterPlanSectionDto> Sections, decimal GrandTotal);

public sealed record CreateMasterPlanItemRequest(
    Guid SectionId, string Name, decimal Price, int Frequency, int SortOrder);

public sealed record UpdateMasterPlanItemRequest(string Name, decimal Price, int Frequency, int SortOrder);

public sealed record UpdateMasterPlanSectionRequest(decimal TargetPercent);
