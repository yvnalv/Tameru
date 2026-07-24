using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Application.Contracts;
using Tameru.Budgeting.Domain;
using Tameru.Modules.Contracts.Ledger;
using Tameru.SharedKernel.Results;

namespace Tameru.Budgeting.Application;

/// <summary>
/// Monthly budget use cases. <em>Plan</em> is stored per category; <em>Actual</em> is derived from
/// the ledger via <see cref="ICategorySpendQuery"/> and <em>Leftover = Plan − Actual</em> (BR-062).
/// </summary>
public sealed class BudgetService
{
    private readonly IBudgetRepository _budgets;
    private readonly ICategoryRepository _categories;
    private readonly ICategorySpendQuery _spend;
    private readonly IBudgetingUnitOfWork _unitOfWork;

    public BudgetService(
        IBudgetRepository budgets,
        ICategoryRepository categories,
        ICategorySpendQuery spend,
        IBudgetingUnitOfWork unitOfWork)
    {
        _budgets = budgets;
        _categories = categories;
        _spend = spend;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<BudgetPeriodSummaryDto>> ListPeriodsAsync(int? year, CancellationToken ct = default)
    {
        var periods = await _budgets.ListPeriodsAsync(year, ct);
        return periods.Select(p => new BudgetPeriodSummaryDto(p.Id, p.Year, p.Month, p.Note)).ToList();
    }

    public async Task<Result<BudgetPeriodDto>> GetPeriodAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _budgets.GetPeriodAsync(year, month, ct);
        return period is null ? BudgetingErrors.PeriodNotFound : await BuildAsync(period, ct);
    }

    public async Task<Result<BudgetPeriodSummaryDto>> CreatePeriodAsync(
        CreateBudgetPeriodRequest request, CancellationToken ct = default)
    {
        if (await _budgets.GetPeriodAsync(request.Year, request.Month, ct) is not null)
        {
            return BudgetingErrors.PeriodExists;
        }

        var period = BudgetPeriod.Create(request.Year, request.Month, request.Note);
        await _budgets.AddPeriodAsync(period, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new BudgetPeriodSummaryDto(period.Id, period.Year, period.Month, period.Note);
    }

    public async Task<Result<BudgetPeriodDto>> UpsertLinesAsync(
        Guid periodId, UpsertBudgetLinesRequest request, CancellationToken ct = default)
    {
        var period = await _budgets.GetPeriodByIdAsync(periodId, ct);
        if (period is null)
        {
            return BudgetingErrors.PeriodNotFound;
        }

        var existing = (await _budgets.ListLinesAsync(periodId, ct)).ToDictionary(l => l.CategoryId);

        foreach (var input in request.Lines)
        {
            if (await _categories.GetByIdAsync(input.CategoryId, ct) is null)
            {
                return BudgetingErrors.CategoryNotFound;
            }

            if (existing.TryGetValue(input.CategoryId, out var line))
            {
                line.SetPlan(input.PlanAmount);
            }
            else
            {
                await _budgets.AddLineAsync(
                    BudgetLine.Create(periodId, input.CategoryId, input.PlanAmount), ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return await BuildAsync(period, ct);
    }

    private async Task<BudgetPeriodDto> BuildAsync(BudgetPeriod period, CancellationToken ct)
    {
        var lines = await _budgets.ListLinesAsync(period.Id, ct);
        var actuals = await _spend.GetExpenseTotalsByCategoryAsync(period.Year, period.Month, ct);
        var categories = await _categories.ListAsync(null, null, null, includeInactive: true, ct);
        var names = categories.ToDictionary(c => c.Id, c => c.Name);

        var lineDtos = lines
            .Select(l =>
            {
                var actual = actuals.GetValueOrDefault(l.CategoryId);
                return new BudgetLineDto(
                    l.CategoryId, names.GetValueOrDefault(l.CategoryId), l.PlanAmount, actual, l.PlanAmount - actual);
            })
            .ToList();

        var totalPlan = lineDtos.Sum(l => l.Plan);
        var totalActual = lineDtos.Sum(l => l.Actual);
        return new BudgetPeriodDto(
            period.Id, period.Year, period.Month, period.Note,
            lineDtos, totalPlan, totalActual, totalPlan - totalActual);
    }
}
