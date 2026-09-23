namespace Tameru.Modules.Contracts.Debts;

/// <summary>
/// Cross-module read query for liabilities. Exposes remaining debt obligations
/// so Reporting can compute true Net Worth (Assets - Liabilities) without coupling
/// to the internal Debts data model (ADR-0005, ADR-0006).
/// </summary>
public interface ILiabilityQuery
{
    /// <summary>
    /// Sum of remaining balances across all active liabilities (Debt + Installment).
    /// Receivables are excluded from this liability total.
    /// </summary>
    Task<decimal> GetTotalRemainingLiabilitiesAsync(CancellationToken ct = default);

    /// <summary>
    /// Total count of active liabilities.
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
}
