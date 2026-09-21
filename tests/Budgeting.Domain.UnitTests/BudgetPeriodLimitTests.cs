using Budgeting.Domain;
using FluentAssertions;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

public class BudgetPeriodLimitTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_ReturnFailure_When_CreatedWithNonPositiveLimit(long amountMinor)
    {
        Result<Budget> result = Budget.Create(Money.Usd(amountMinor), Budgets.January);

        result.Error.Should().Be(BudgetErrors.NonPositiveLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_ReturnFailure_When_ChangingLimitToNonPositiveAmount(long amountMinor)
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ChangeLimit(Money.Usd(amountMinor));

        result.Error.Should().Be(BudgetErrors.NonPositiveLimit);
    }

    [Fact]
    public void Should_Succeed_When_CreatedWithValidLimit()
    {
        Result<Budget> result = Budget.Create(Money.Usd(1_000_000), Budgets.January);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_HaveZeroSpentInLimitCurrency_When_CreatedWithValidLimit()
    {
        Money limit = Money.Eur(1_000_000);

        Budget budget = Budget.Create(limit, Budgets.January).Value;

        Budgets.Period(budget).Spent.Should().Be(Money.Eur(0));
    }

    [Fact]
    public void Should_ReturnFailure_When_ChangingLimitOnClosedPeriod()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ClosePeriod();

        Result result = budget.ChangeLimit(Money.Usd(1_200_000));

        result.Error.Should().Be(BudgetErrors.ClosedPeriodLimit);
    }

    [Fact]
    public void Should_ReturnCurrencyMismatch_When_ChangingLimitWithDifferentCurrency()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ChangeLimit(Money.Eur(1_200_000));

        result.Error.Should().Be(BudgetErrors.CurrencyMismatch);
    }

    [Fact]
    public void Should_Succeed_When_ApplyingChargeToClosedPeriod()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ClosePeriod();

        Result result = budget.ApplyCharge(Budgets.Ec2(15), Money.Usd(100_000));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_ChangeSpent_When_ApplyingChargeToClosedPeriod()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ClosePeriod();

        budget.ApplyCharge(Budgets.Ec2(15), Money.Usd(100_000));

        Budgets.Period(budget).Spent.Should().Be(Money.Usd(100_000));
    }

    [Fact]
    public void Should_Succeed_When_ChangingLimitOnOpenPeriod()
    {
        Budget budget = Budgets.OpenJanuary();

        Result result = budget.ChangeLimit(Money.Usd(1_200_000));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_RaiseLimitChanged_When_ChangingLimitOnOpenPeriod()
    {
        Budget budget = Budgets.OpenJanuary();
        Money previousLimit = Budgets.Period(budget).Limit;
        Money newLimit = Money.Usd(1_200_000);

        budget.ChangeLimit(newLimit);

        budget.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LimitChanged>()
            .Which.Should().Match<LimitChanged>(changed =>
                changed.BudgetId == budget.Id
                && changed.PreviousLimit == previousLimit
                && changed.NewLimit == newLimit);
    }

    [Fact]
    public void Should_RaisePeriodClosed_When_PeriodIsClosed()
    {
        Budget budget = Budgets.OpenJanuary();
        BudgetPeriod period = Budgets.Period(budget);

        budget.ClosePeriod();

        budget.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PeriodClosed>()
            .Which.Should().Match<PeriodClosed>(closed =>
                closed.BudgetId == budget.Id
                && closed.Range == period.Range
                && closed.Limit == period.Limit
                && closed.Spent == period.Spent);
    }

    [Fact]
    public void Should_ReturnAlreadyClosed_When_ClosingAlreadyClosedPeriod()
    {
        Budget budget = Budgets.OpenJanuary();
        budget.ClosePeriod();

        Result result = budget.ClosePeriod();

        result.Error.Should().Be(BudgetErrors.AlreadyClosed);
    }
}
