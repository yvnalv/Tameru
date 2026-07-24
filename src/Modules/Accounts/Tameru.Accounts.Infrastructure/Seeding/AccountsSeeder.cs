using Tameru.Accounts.Application.Abstractions;
using Tameru.Accounts.Domain;

namespace Tameru.Accounts.Infrastructure.Seeding;

/// <summary>
/// Seeds the default account groups from the workbook on first run (idempotent). Accounts themselves
/// are created by the owner (or imported), so only groups are seeded.
/// </summary>
public sealed class AccountsSeeder
{
    private static readonly string[] DefaultGroups =
    [
        "Saving", "Investment", "Family", "Personal", "Subscription", "Transportation", "Eats",
    ];

    private readonly IAccountGroupRepository _groups;
    private readonly IAccountsUnitOfWork _unitOfWork;

    public AccountsSeeder(IAccountGroupRepository groups, IAccountsUnitOfWork unitOfWork)
    {
        _groups = groups;
        _unitOfWork = unitOfWork;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _groups.AnyAsync(cancellationToken))
        {
            return;
        }

        for (var i = 0; i < DefaultGroups.Length; i++)
        {
            await _groups.AddAsync(AccountGroup.Create(DefaultGroups[i], i), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
