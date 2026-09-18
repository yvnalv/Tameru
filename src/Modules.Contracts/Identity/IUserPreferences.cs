namespace Tameru.Modules.Contracts.Identity;

/// <summary>
/// Cross-module read contract for user preferences such as financial cycle starting day.
/// </summary>
public interface IUserPreferences
{
    Task<int> GetBudgetCycleStartDayAsync(CancellationToken ct = default);
}
