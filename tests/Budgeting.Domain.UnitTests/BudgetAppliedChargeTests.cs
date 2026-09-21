using Budgeting.Domain;
using FluentAssertions;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

public class BudgetAppliedChargeTests
{
    [Fact]
    public void Should_LeaveSpentUnchanged_When_SameChargeKeyAppliedTwiceWithSameAmount()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(100_000));

        budget.ApplyCharge(key, Money.Usd(100_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(100_000));
    }

    [Fact]
    public void Should_StoreTwoCharges_When_SameServiceHasDifferentUsageDates()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey januaryTenth = Budgets.Ec2(10);
        ChargeKey januaryEleventh = Budgets.Ec2(11);

        budget.ApplyCharge(januaryTenth, Money.Usd(100_000));
        budget.ApplyCharge(januaryEleventh, Money.Usd(40_000));

        Budgets.Period(budget).AppliedCharges.Select(charge => charge.Key)
            .Should().BeEquivalentTo([januaryTenth, januaryEleventh]);
    }

    [Fact]
    public void Should_SumBothAmounts_When_SameServiceHasDifferentUsageDates()
    {
        Budget budget = Budgets.OpenJanuary();

        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(100_000));
        budget.ApplyCharge(Budgets.Ec2(11), Money.Usd(40_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(140_000));
    }

    [Fact]
    public void Should_StoreTwoCharges_When_DifferentServicesShareUsageDate()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey ec2 = Budgets.Ec2(10);
        ChargeKey s3 = Budgets.S3(10);

        budget.ApplyCharge(ec2, Money.Usd(100_000));
        budget.ApplyCharge(s3, Money.Usd(50_000));

        Budgets.Period(budget).AppliedCharges.Select(charge => charge.Key)
            .Should().BeEquivalentTo([ec2, s3]);
    }

    [Fact]
    public void Should_IncreaseSpentByDelta_When_ChargeIsCorrectedUp()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey ec2 = Budgets.Ec2(10);
        budget.ApplyCharge(ec2, Money.Usd(100_000));
        budget.ApplyCharge(Budgets.S3(10), Money.Usd(50_000));

        budget.ApplyCharge(ec2, Money.Usd(130_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(180_000));
    }

    [Fact]
    public void Should_DecreaseSpentByDelta_When_ChargeIsCorrectedDown()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey ec2 = Budgets.Ec2(10);
        budget.ApplyCharge(ec2, Money.Usd(100_000));
        budget.ApplyCharge(Budgets.S3(10), Money.Usd(50_000));

        budget.ApplyCharge(ec2, Money.Usd(70_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(120_000));
    }

    [Fact]
    public void Should_NotRaiseThresholdAlert_When_CorrectionDropsBelowThreshold()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(790_000));

        budget.DomainEvents.OfType<ThresholdAlertRaised>().Should().BeEmpty();
    }

    [Fact]
    public void Should_AcceptCharge_When_UsageDateIsPeriodStart()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ApplyCharge(
            Budgets.Ec2On(new DateOnly(2026, 1, 1)),
            Money.Usd(10_000));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_AcceptCharge_When_UsageDateIsLastDayOfPeriod()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ApplyCharge(
            Budgets.Ec2On(new DateOnly(2026, 1, 31)),
            Money.Usd(10_000));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_ReturnFailure_When_UsageDateIsDayBeforePeriod()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ApplyCharge(
            Budgets.Ec2On(new DateOnly(2025, 12, 31)),
            Money.Usd(10_000));

        result.Error.Should().Be(BudgetErrors.UsageDateOutsidePeriod);
    }

    [Fact]
    public void Should_ReturnFailure_When_UsageDateIsDayAfterPeriod()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ApplyCharge(
            Budgets.Ec2On(new DateOnly(2026, 2, 1)),
            Money.Usd(10_000));

        result.Error.Should().Be(BudgetErrors.UsageDateOutsidePeriod);
    }

    [Fact]
    public void Should_ReturnFailure_When_ChargeCurrencyDiffersFromLimit()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(100_000));

        Result result = budget.ApplyCharge(Budgets.S3(10), Money.Eur(50_000));

        result.Error.Should().Be(BudgetErrors.CurrencyMismatch);
    }

    [Fact]
    public void Should_LeaveSpentUnchanged_When_ChargeCurrencyDiffersFromLimit()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(100_000));

        budget.ApplyCharge(Budgets.S3(10), Money.Eur(50_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(100_000));
    }

    [Fact]
    public void Should_Succeed_When_ChargeAmountIsZero()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(0));

        result.IsSuccess.Should().BeTrue();
    }
}
