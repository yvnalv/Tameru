using Tameru.Modules.Contracts.Ledger;

namespace Tameru.Accounts.Infrastructure;

/// <summary>
/// Default <see cref="ILedgerAccountQuery"/> used until the Ledger module (M3) ships: reports zero
/// movement and no usage, so balances equal opening balances and any account may be deactivated.
/// Registered via <c>TryAdd</c> so Ledger can replace it when present.
/// </summary>
internal sealed class NoOpLedgerAccountQuery : ILedgerAccountQuery
{
    private static readonly IReadOnlyDictionary<Guid, decimal> Empty =
        new Dictionary<Guid, decimal>();

    public Task<IReadOnlyDictionary<Guid, decimal>> GetNetMovementByAccountAsync(
        DateOnly? asOf = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Empty);

    public Task<decimal> GetNetMovementAsync(
        Guid accountId, DateOnly? asOf = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);

    public Task<bool> HasTransactionsAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
