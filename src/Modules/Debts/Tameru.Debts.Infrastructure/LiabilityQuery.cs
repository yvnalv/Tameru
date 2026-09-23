using Tameru.Debts.Application.Abstractions;
using Tameru.Modules.Contracts.Debts;

namespace Tameru.Debts.Infrastructure;

public sealed class LiabilityQuery : ILiabilityQuery
{
    private readonly ILiabilityRepository _repository;

    public LiabilityQuery(ILiabilityRepository repository)
    {
        _repository = repository;
    }

    public Task<decimal> GetTotalRemainingLiabilitiesAsync(CancellationToken ct = default)
    {
        return _repository.GetTotalRemainingActiveAsync(ct);
    }

    public Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        return _repository.GetActiveCountAsync(ct);
    }
}
