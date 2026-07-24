using Microsoft.Extensions.Options;
using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Domain;

namespace Tameru.Identity.Infrastructure.Seeding;

/// <summary>
/// Creates the single owner account on first run (idempotent — does nothing if any user exists).
/// Single-user app (ADR-0001), so there is exactly one seeded user.
/// </summary>
public sealed class IdentitySeeder
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly OwnerSeedOptions _options;

    public IdentitySeeder(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IIdentityUnitOfWork unitOfWork,
        IOptions<OwnerSeedOptions> options)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _users.AnyAsync(cancellationToken))
        {
            return;
        }

        var owner = User.Create(
            _options.Email,
            _passwordHasher.Hash(_options.Password),
            _options.DisplayName,
            _options.Locale);

        await _users.AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
