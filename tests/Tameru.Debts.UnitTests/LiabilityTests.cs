using FluentAssertions;
using Tameru.Debts.Domain;
using Tameru.SharedKernel.Domain;
using Xunit;

namespace Tameru.Debts.UnitTests;

public class LiabilityTests
{
    [Fact]
    public void Create_with_valid_parameters_sets_initial_state()
    {
        var liability = Liability.Create(
            title: "KPR Rumah",
            type: LiabilityType.Debt,
            creditor: "Bank BTN",
            totalAmount: 120_000_000m,
            initialPaidAmount: 20_000_000m,
            monthlyInstallment: 3_500_000m,
            dueDay: 15,
            interestRate: 6.5m);

        liability.Title.Should().Be("KPR Rumah");
        liability.Type.Should().Be(LiabilityType.Debt);
        liability.Creditor.Should().Be("Bank BTN");
        liability.TotalAmount.Should().Be(120_000_000m);
        liability.PaidAmount.Should().Be(20_000_000m);
        liability.RemainingBalance.Should().Be(100_000_000m);
        liability.MonthlyInstallment.Should().Be(3_500_000m);
        liability.DueDay.Should().Be(15);
        liability.InterestRate.Should().Be(6.5m);
        liability.Status.Should().Be(LiabilityStatus.Active);
    }

    [Fact]
    public void Create_with_zero_or_negative_amount_throws_domain_exception()
    {
        var act = () => Liability.Create(
            title: "Cicilan",
            type: LiabilityType.Installment,
            creditor: "Vendor",
            totalAmount: 0m);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("*must be greater than zero*");
    }

    [Fact]
    public void Create_with_invalid_type_throws_domain_exception()
    {
        var act = () => Liability.Create(
            title: "Cicilan",
            type: "InvalidType",
            creditor: "Vendor",
            totalAmount: 5_000_000m);

        act.Should().Throw<DomainRuleException>()
            .WithMessage("*Invalid liability type*");
    }

    [Fact]
    public void RecordPayment_increments_paid_and_decrements_remaining_balance()
    {
        var liability = Liability.Create(
            title: "Cicilan Laptop",
            type: LiabilityType.Installment,
            creditor: "BCA Finance",
            totalAmount: 24_000_000m,
            monthlyInstallment: 2_000_000m);

        var payment = liability.RecordPayment(
            amount: 2_000_000m,
            date: new DateOnly(2026, 9, 20),
            principal: 2_000_000m,
            interest: 0m,
            notes: "Month 1 payment");

        payment.Should().NotBeNull();
        liability.PaidAmount.Should().Be(2_000_000m);
        liability.RemainingBalance.Should().Be(22_000_000m);
        liability.Status.Should().Be(LiabilityStatus.Active);
        liability.Payments.Should().HaveCount(1);
    }

    [Fact]
    public void Full_repayment_automatically_marks_status_as_paid_off()
    {
        var liability = Liability.Create(
            title: "Pinjaman Teman",
            type: LiabilityType.Debt,
            creditor: "Budi",
            totalAmount: 5_000_000m);

        liability.RecordPayment(5_000_000m, new DateOnly(2026, 9, 23));

        liability.RemainingBalance.Should().Be(0m);
        liability.PaidAmount.Should().Be(5_000_000m);
        liability.Status.Should().Be(LiabilityStatus.PaidOff);
    }
}
