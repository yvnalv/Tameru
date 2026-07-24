using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Domain;
using Tameru.SharedKernel.Time;

namespace Tameru.Identity.UnitTests;

internal sealed class TestClock : IClock
{
    public TestClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public FakeUserRepository(params User[] seed) => _users.AddRange(seed);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<bool> AnyAsync(CancellationToken ct = default) => Task.FromResult(_users.Count > 0);

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = new();

    public Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.RevokedAt is null));

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string passwordHash, string providedPassword) =>
        passwordHash == "hashed:" + providedPassword;
}

internal sealed class FakeTokenService : ITokenService
{
    private readonly IClock _clock;

    public FakeTokenService(IClock clock) => _clock = clock;

    public AccessToken CreateAccessToken(User user) =>
        new("access-" + user.Id, _clock.UtcNow.AddMinutes(15));

    public RefreshTokenValue CreateRefreshToken()
    {
        var raw = Guid.NewGuid().ToString("N");
        return new RefreshTokenValue(raw, HashRefreshToken(raw), _clock.UtcNow.AddDays(14));
    }

    public string HashRefreshToken(string rawToken) => "H:" + rawToken;
}

internal sealed class FakeIdentityUnitOfWork : IIdentityUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}
