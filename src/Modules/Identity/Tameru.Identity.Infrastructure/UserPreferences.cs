using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.Identity.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Identity;

namespace Tameru.Identity.Infrastructure;

public sealed class UserPreferences : IUserPreferences
{
    private readonly IdentityDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UserPreferences(IdentityDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<int> GetBudgetCycleStartDayAsync(CancellationToken ct = default)
    {
        int day = 1;
        if (_currentUser.UserId != Guid.Empty)
        {
            day = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == _currentUser.UserId)
                .Select(u => u.BudgetCycleStartDay)
                .FirstOrDefaultAsync(ct);
        }

        if (day <= 0)
        {
            day = await _dbContext.Users
                .AsNoTracking()
                .Select(u => u.BudgetCycleStartDay)
                .FirstOrDefaultAsync(ct);
        }

        return day is >= 1 and <= 31 ? day : 1;
    }
}
