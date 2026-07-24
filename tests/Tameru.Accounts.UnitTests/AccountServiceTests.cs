using FluentAssertions;
using Tameru.Accounts.Application;
using Tameru.Accounts.Application.Contracts;
using Tameru.Accounts.Domain;
using Xunit;

namespace Tameru.Accounts.UnitTests;

public class AccountServiceTests
{
    private readonly FakeAccountRepository _accounts = new();
    private readonly FakeAccountGroupRepository _groups = new();
    private readonly StubLedgerAccountQuery _ledger = new();
    private readonly FakeAccountsUnitOfWork _uow = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(_accounts, _groups, _ledger, _uow);
    }

    [Fact]
    public async Task Create_persists_account_and_returns_balance_from_opening()
    {
        var result = await _sut.CreateAsync(
            new CreateAccountRequest("BCA", "Bank", 81_297.48m, null, "IDR", 1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Balance.Should().Be(81_297.48m);
        _accounts.Items.Should().ContainSingle();
        _uow.SaveCalls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_with_invalid_type_fails_validation()
    {
        var result = await _sut.CreateAsync(
            new CreateAccountRequest("X", "Crypto", 0m, null, "IDR", 0));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation_error");
    }

    [Fact]
    public async Task Create_with_unknown_group_fails_not_found()
    {
        var result = await _sut.CreateAsync(
            new CreateAccountRequest("BCA", "Bank", 0m, Guid.NewGuid(), "IDR", 0));

        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task List_balance_is_opening_plus_ledger_movement()
    {
        var account = Account.Create("BCA", AccountType.Bank, 100_000m);
        _accounts.Items.Add(account);
        _ledger.WithMovement(account.Id, 25_000m);

        var list = await _sut.ListAsync(includeInactive: false);

        list.Should().ContainSingle();
        list[0].Balance.Should().Be(125_000m);
    }

    [Fact]
    public async Task Deactivate_is_blocked_when_account_has_transactions()
    {
        var account = Account.Create("BCA", AccountType.Bank);
        _accounts.Items.Add(account);
        _ledger.WithTransactions(account.Id);

        var result = await _sut.DeactivateAsync(account.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("account_in_use");
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_succeeds_when_account_is_unused()
    {
        var account = Account.Create("BCA", AccountType.Bank);
        _accounts.Items.Add(account);

        var result = await _sut.DeactivateAsync(account.Id);

        result.IsSuccess.Should().BeTrue();
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ListGroups_rolls_up_member_balances()
    {
        var group = AccountGroup.Create("Saving");
        _groups.Items.Add(group);
        var a1 = Account.Create("BCA", AccountType.Bank, 100_000m, group.Id);
        var a2 = Account.Create("Cash", AccountType.Cash, 20_000m, group.Id);
        _accounts.Items.Add(a1);
        _accounts.Items.Add(a2);
        _ledger.WithMovement(a1.Id, 5_000m);

        var groups = await _sut.ListGroupsAsync();

        groups.Should().ContainSingle();
        groups[0].AccountCount.Should().Be(2);
        groups[0].TotalBalance.Should().Be(125_000m);
    }
}
