using FluentAssertions;
using Tameru.Ledger.Application;
using Tameru.Ledger.Application.Contracts;
using Tameru.Ledger.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Ledger.UnitTests;

public class LedgerServiceTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 25);

    private readonly FakeTransactionRepository _repo = new();
    private readonly FakeLedgerUnitOfWork _uow = new();
    private readonly LedgerService _sut;

    public LedgerServiceTests()
    {
        _sut = new LedgerService(_repo, new FakeAccountDirectory(A, B), new FakeCategoryDirectory(), _uow);
    }

    private static CreateTransactionRequest Income(decimal amount, Guid account) =>
        new("Income", Today, "Salary", amount, account, null, null, null, null, "Cleared", "IDR", null);

    private static CreateTransactionRequest Transfer(decimal amount, Guid from, Guid to) =>
        new("Transfer", Today, "Move", amount, from, to, null, null, null, null, "IDR", null);

    [Fact]
    public async Task Create_income_persists_and_returns_dto()
    {
        var result = await _sut.CreateAsync(Income(15_000_000m, A));

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be("Income");
        result.Value.Status.Should().Be("Cleared");
        _repo.Items.Should().ContainSingle();
        _uow.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Create_with_invalid_type_fails_validation()
    {
        var request = Income(1m, A) with { Type = "Refund" };

        var result = await _sut.CreateAsync(request);

        result.Error.Code.Should().Be("validation_error");
    }

    [Fact]
    public async Task Create_with_unknown_account_fails()
    {
        var result = await _sut.CreateAsync(Income(1m, Guid.NewGuid()));

        result.Error.Code.Should().Be("account_not_found");
        _repo.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_transfer_requires_both_accounts_to_exist()
    {
        var result = await _sut.CreateAsync(Transfer(100m, A, Guid.NewGuid()));

        result.Error.Code.Should().Be("account_not_found");
    }

    [Fact]
    public async Task Create_transfer_between_known_accounts_succeeds()
    {
        var result = await _sut.CreateAsync(Transfer(7_300_000m, A, B));

        result.IsSuccess.Should().BeTrue();
        result.Value.ToAccountId.Should().Be(B);
    }

    [Fact]
    public async Task Create_with_non_positive_amount_throws_domain_rule()
    {
        var act = async () => await _sut.CreateAsync(Income(0m, A));

        (await act.Should().ThrowAsync<DomainRuleException>()).Which.Code.Should().Be("amount_not_positive");
    }

    [Fact]
    public async Task Void_removes_the_transaction()
    {
        var created = await _sut.CreateAsync(Income(100m, A));
        var id = created.Value.Id;

        var result = await _sut.VoidAsync(id);

        result.IsSuccess.Should().BeTrue();
        _repo.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Clear_sets_status_to_cleared()
    {
        var created = await _sut.CreateAsync(Income(100m, A) with { Status = "Uncleared" });
        var id = created.Value.Id;

        var result = await _sut.ClearAsync(id);

        result.Value.Status.Should().Be("Cleared");
    }

    [Fact]
    public async Task Void_unknown_transaction_returns_not_found()
    {
        var result = await _sut.VoidAsync(Guid.NewGuid());

        result.Error.Code.Should().Be("not_found");
    }
}
