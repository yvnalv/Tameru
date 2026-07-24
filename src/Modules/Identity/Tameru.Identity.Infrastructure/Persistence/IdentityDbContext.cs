using Microsoft.EntityFrameworkCore;
using Tameru.Application.Abstractions;
using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Domain;
using Tameru.Identity.Infrastructure.Persistence.Configurations;
using Tameru.Infrastructure.Common.Persistence;
using Tameru.SharedKernel.Time;

namespace Tameru.Identity.Infrastructure.Persistence;

/// <summary>EF Core context for the Identity module. Owns the <c>identity</c> schema.</summary>
public sealed class IdentityDbContext : BaseDbContext, IIdentityUnitOfWork
{
    public const string Schema = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUser currentUser, IClock clock)
        : base(options, currentUser, clock)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());

        ApplySoftDeleteFilter(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
