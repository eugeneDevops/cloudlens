using Budgeting.Domain;
using FluentAssertions;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

public class BudgetThresholdTests
{
    [Fact]
    public void Should_RaiseSingleThresholdAlert_When_UtilizationReachesFiftyPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(400_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(500_000));

        ShouldRaiseSingleAlert(budget, Threshold.Fifty);
    }

    [Fact]
    public void Should_RaiseSingleThresholdAlert_When_UtilizationReachesEightyPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(600_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(800_000));

        ShouldRaiseSingleAlert(budget, Threshold.Eighty);
    }

    [Fact]
    public void Should_RaiseSingleThresholdAlert_When_UtilizationReachesHundredPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(1_000_000));

        ShouldRaiseSingleAlert(budget, Threshold.Hundred);
    }

    [Fact]
    public void Should_NotRaiseEvents_When_SpendIncreasesFurtherAboveEightyPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(850_000));

        budget.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_KeepEightyPercentLatched_When_UtilizationDropsToSeventyNinePercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(790_000));

        EightyPercent(budget).Status.Should().Be(ThresholdStatus.Latched);
    }

    [Fact]
    public void Should_NotRaiseEvents_When_UtilizationDropsToSeventyNinePercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(790_000));

        budget.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Should_ResetThreshold_When_UtilizationDropsToSeventyFivePercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(750_000));

        ShouldRaiseSingleReset(budget, Threshold.Eighty);
    }

    [Fact]
    public void Should_ResetThreshold_When_UtilizationDropsToSeventyFourPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(740_000));

        ShouldRaiseSingleReset(budget, Threshold.Eighty);
    }

    [Fact]
    public void Should_RaiseThresholdAlertAgain_When_UtilizationReachesEightyPercentAfterReset()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));
        budget.ApplyCharge(key, Money.Usd(740_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(800_000));

        ShouldRaiseSingleAlert(budget, Threshold.Eighty);
    }

    [Fact]
    public void Should_TreatExactEightyPercentAsThresholdCrossing()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(790_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(key, Money.Usd(800_000));

        ShouldRaiseSingleAlert(budget, Threshold.Eighty);
    }

    [Fact]
    public void Should_RaiseThreeThresholdAlertsInAscendingOrder_When_OneChargeCrossesAllThresholds()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(400_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(Budgets.Ec2(11), Money.Usd(1_100_000));

        budget.DomainEvents.Should().SatisfyRespectively(
            domainEvent => ShouldBeAlert(domainEvent, budget.Id, Threshold.Fifty),
            domainEvent => ShouldBeAlert(domainEvent, budget.Id, Threshold.Eighty),
            domainEvent => ShouldBeAlert(domainEvent, budget.Id, Threshold.Hundred));
    }

    [Fact]
    public void Should_AssignDistinctEventIds_When_OneChargeCrossesAllThresholds()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(400_000));
        budget.ClearDomainEvents();

        budget.ApplyCharge(Budgets.Ec2(11), Money.Usd(1_100_000));

        budget.DomainEvents.Select(domainEvent => domainEvent.EventId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Should_KeepEightyPercentLatched_When_LimitIncreaseDropsUtilizationToSeventyEightPercent()
    {
        Money originalLimit = Money.Usd(390_000);
        Budget budget = Budgets.OpenJanuary(originalLimit);
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(312_000));
        budget.ClearDomainEvents();

        budget.ChangeLimit(Money.Usd(400_000));

        EightyPercent(budget).Status.Should().Be(ThresholdStatus.Latched);
    }

    [Fact]
    public void Should_RaiseHundredPercentAlert_When_LimitDecreaseDrivesUtilizationAboveHundredPercent()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(800_000));
        budget.ClearDomainEvents();

        budget.ChangeLimit(Money.Usd(700_000));

        budget.DomainEvents.OfType<ThresholdAlertRaised>().Should().ContainSingle()
            .Which.Should().Match<ThresholdAlertRaised>(raised =>
                raised.BudgetId == budget.Id && raised.Threshold == Threshold.Hundred);
    }

    [Fact]
    public void Should_ResetEightyPercentThreshold_When_LimitIncreaseDropsUtilizationToSixtySevenPercent()
    {
        Money originalLimit = Money.Usd(670_000);
        Budget budget = Budgets.OpenJanuary(originalLimit);
        budget.ApplyCharge(Budgets.Ec2(10), Money.Usd(536_000));
        budget.ClearDomainEvents();

        budget.ChangeLimit(Money.Usd(800_000));

        budget.DomainEvents.OfType<ThresholdReset>().Should().ContainSingle()
            .Which.Should().Match<ThresholdReset>(reset =>
                reset.BudgetId == budget.Id && reset.Threshold == Threshold.Eighty);
    }

    [Fact]
    public void Should_ClearAllThresholds_When_SpentIsNegative()
    {
        Budget budget = Budgets.OpenJanuary();
        ChargeKey key = Budgets.Ec2(10);
        budget.ApplyCharge(key, Money.Usd(800_000));

        budget.ApplyCharge(key, Money.Usd(-100_000));

        Budgets.Period(budget).Thresholds
            .Should().HaveCount(3)
            .And.OnlyContain(state => state.Status == ThresholdStatus.Clear);
    }

    private static ThresholdState EightyPercent(Budget budget) =>
        Budgets.Period(budget).Thresholds.Single(state => state.Threshold == Threshold.Eighty);

    private static void ShouldRaiseSingleAlert(Budget budget, Threshold threshold)
    {
        IDomainEvent domainEvent = budget.DomainEvents.Should().ContainSingle().Subject;
        ShouldBeAlert(domainEvent, budget.Id, threshold);
    }

    private static void ShouldRaiseSingleReset(Budget budget, Threshold threshold)
    {
        budget.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ThresholdReset>()
            .Which.Should().Match<ThresholdReset>(reset =>
                reset.BudgetId == budget.Id && reset.Threshold == threshold);
    }

    private static void ShouldBeAlert(IDomainEvent domainEvent, Guid budgetId, Threshold threshold)
    {
        domainEvent.Should().BeOfType<ThresholdAlertRaised>()
            .Which.Should().Match<ThresholdAlertRaised>(raised =>
                raised.BudgetId == budgetId && raised.Threshold == threshold);
    }
}
