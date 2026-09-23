using FluentAssertions;
using Tameru.Debts.Application;
using Tameru.Debts.Application.Abstractions;
using Tameru.Debts.Application.Contracts;
using Tameru.Debts.Domain;
using Xunit;

namespace Tameru.Debts.UnitTests;

public class DebtsServiceTests
{
    private sealed class InMemoryLiabilityRepository : ILiabilityRepository
    {
        public readonly List<Liability> Items = [];

        public Task<IReadOnlyList<Liability>> ListAsync(string? status = null, string? type = null, CancellationToken ct = default)
        {
            var query = Items.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);
            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(i => i.Type == type);
            return Task.FromResult<IReadOnlyList<Liability>>(query.ToList());
        }

        public Task<Liability?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task AddAsync(Liability liability, CancellationToken ct = default)
        {
            Items.Add(liability);
            return Task.CompletedTask;
        }

        public void Remove(Liability liability) => Items.Remove(liability);

        public Task<decimal> GetTotalRemainingActiveAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.Where(i => i.Status == LiabilityStatus.Active && i.Type != LiabilityType.Receivable).Sum(i => i.RemainingBalance));

        public Task<int> GetActiveCountAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.Count(i => i.Status == LiabilityStatus.Active && i.Type != LiabilityType.Receivable));

        public Task AddPaymentAsync(LiabilityPayment payment, CancellationToken ct = default) => Task.CompletedTask;

        public void RemovePayment(LiabilityPayment payment) { }
    }

    private sealed class InMemoryUnitOfWork : IDebtsUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    [Fact]
    public async Task GetSummaryAsync_computes_correct_totals_and_progress()
    {
        var repo = new InMemoryLiabilityRepository();
        var uow = new InMemoryUnitOfWork();
        var service = new DebtsService(repo, uow);

        // Debt 1: 10M total, 4M paid = 6M remaining, active, monthly 1M
        var debt1 = Liability.Create("Kredit Mobil", LiabilityType.Debt, "BCA", 10_000_000m, 4_000_000m, 1_000_000m);
        // Debt 2: 5M total, 5M paid = 0M remaining, paid off
        var debt2 = Liability.Create("Cicilan TV", LiabilityType.Installment, "Home Credit", 5_000_000m, 5_000_000m);
        // Receivable: 2M owed to user
        var rec = Liability.Create("Pinjaman Budi", LiabilityType.Receivable, "Budi", 2_000_000m, 500_000m);

        repo.Items.AddRange([debt1, debt2, rec]);

        var result = await service.GetSummaryAsync();

        result.IsSuccess.Should().BeTrue();
        var summary = result.Value;
        summary.TotalDebts.Should().Be(15_000_000m);
        summary.TotalPaid.Should().Be(9_000_000m);
        summary.TotalRemaining.Should().Be(6_000_000m);
        summary.MonthlyCommitment.Should().Be(1_000_000m);
        summary.TotalReceivables.Should().Be(1_500_000m);
        summary.ActiveCount.Should().Be(2); // debt1 + rec
        summary.PaidOffCount.Should().Be(1); // debt2
        summary.OverallProgressPercentage.Should().Be(60.0m);
    }

    [Fact]
    public async Task RecordPaymentAsync_updates_balance_and_records_payment_history()
    {
        var repo = new InMemoryLiabilityRepository();
        var uow = new InMemoryUnitOfWork();
        var service = new DebtsService(repo, uow);

        var liability = Liability.Create("Cicilan Handphone", LiabilityType.Installment, "Akulaku", 12_000_000m, 0m, 1_000_000m);
        repo.Items.Add(liability);

        var paymentResult = await service.RecordPaymentAsync(
            liability.Id,
            new RecordLiabilityPaymentRequest(1_000_000m, new DateOnly(2026, 9, 23), Notes: "Month 1"));

        paymentResult.IsSuccess.Should().BeTrue();
        paymentResult.Value.Amount.Should().Be(1_000_000m);

        var updated = await service.GetByIdAsync(liability.Id);
        updated.Value.PaidAmount.Should().Be(1_000_000m);
        updated.Value.RemainingBalance.Should().Be(11_000_000m);
        updated.Value.ProgressPercentage.Should().Be(8.3m);
    }
}
