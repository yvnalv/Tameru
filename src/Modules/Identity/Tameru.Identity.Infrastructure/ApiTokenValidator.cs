using Microsoft.EntityFrameworkCore;
using Tameru.Identity.Infrastructure.Persistence;
using Tameru.Modules.Contracts.Identity;

namespace Tameru.Identity.Infrastructure;

public sealed class ApiTokenValidator : IApiTokenValidator
{
    private readonly IdentityDbContext _dbContext;

    public ApiTokenValidator(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.ApiToken == token.Trim(), ct);
    }
}
