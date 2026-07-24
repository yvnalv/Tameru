using Tameru.Budgeting.Application.Abstractions;
using Tameru.Budgeting.Application.Contracts;
using Tameru.Budgeting.Domain;
using Tameru.SharedKernel.Results;

namespace Tameru.Budgeting.Application;

/// <summary>
/// Master Plan use cases: Investment / Needs / Wants sections and their items. Each item's total is
/// <c>Price × Frequency</c> (BR-080); sections carry a target % (default 40/50/10, BR-081).
/// </summary>
public sealed class MasterPlanService
{
    private readonly IMasterPlanRepository _repository;
    private readonly IBudgetingUnitOfWork _unitOfWork;

    public MasterPlanService(IMasterPlanRepository repository, IBudgetingUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MasterPlanDto> GetAsync(CancellationToken ct = default)
    {
        var sections = await _repository.ListSectionsAsync(ct);
        var items = await _repository.ListItemsAsync(ct);
        var bySection = items.GroupBy(i => i.SectionId).ToDictionary(g => g.Key, g => g.ToList());

        var sectionDtos = sections
            .Select(s =>
            {
                var sectionItems = bySection.GetValueOrDefault(s.Id, new List<MasterPlanItem>());
                var itemDtos = sectionItems
                    .OrderBy(i => i.SortOrder)
                    .Select(MapItem)
                    .ToList();
                return new MasterPlanSectionDto(
                    s.Id, s.Name, s.TargetPercent, s.SortOrder, itemDtos, itemDtos.Sum(i => i.TotalBudget));
            })
            .ToList();

        return new MasterPlanDto(sectionDtos, sectionDtos.Sum(s => s.Total));
    }

    public async Task<Result<MasterPlanItemDto>> CreateItemAsync(
        CreateMasterPlanItemRequest request, CancellationToken ct = default)
    {
        if (await _repository.GetSectionAsync(request.SectionId, ct) is null)
        {
            return BudgetingErrors.SectionNotFound;
        }

        var item = MasterPlanItem.Create(
            request.SectionId, request.Name, request.Price, request.Frequency, request.SortOrder);
        await _repository.AddItemAsync(item, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapItem(item);
    }

    public async Task<Result<MasterPlanItemDto>> UpdateItemAsync(
        Guid id, UpdateMasterPlanItemRequest request, CancellationToken ct = default)
    {
        var item = await _repository.GetItemAsync(id, ct);
        if (item is null)
        {
            return BudgetingErrors.ItemNotFound;
        }

        item.Update(request.Name, request.Price, request.Frequency, request.SortOrder);
        await _unitOfWork.SaveChangesAsync(ct);
        return MapItem(item);
    }

    public async Task<Result> DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repository.GetItemAsync(id, ct);
        if (item is null)
        {
            return BudgetingErrors.ItemNotFound;
        }

        _repository.RemoveItem(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<MasterPlanSectionDto>> UpdateSectionAsync(
        Guid id, UpdateMasterPlanSectionRequest request, CancellationToken ct = default)
    {
        var section = await _repository.GetSectionAsync(id, ct);
        if (section is null)
        {
            return BudgetingErrors.SectionNotFound;
        }

        section.SetTarget(request.TargetPercent);
        await _unitOfWork.SaveChangesAsync(ct);

        var items = (await _repository.ListItemsAsync(ct))
            .Where(i => i.SectionId == id).OrderBy(i => i.SortOrder).Select(MapItem).ToList();
        return new MasterPlanSectionDto(
            section.Id, section.Name, section.TargetPercent, section.SortOrder, items, items.Sum(i => i.TotalBudget));
    }

    private static MasterPlanItemDto MapItem(MasterPlanItem i) => new(
        i.Id, i.SectionId, i.Name, i.Price, i.Frequency, i.TotalBudget, i.SortOrder);
}
