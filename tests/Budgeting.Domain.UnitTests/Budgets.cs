using Budgeting.Domain;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

internal static class Budgets
{
    public static readonly PeriodRange January = PeriodRange.Create(
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 2, 1)).Value;

    public static readonly Money TenThousandUsd = Money.Usd(1_000_000);

    public static readonly AccountId Account = AccountId.Create("123456789012").Value;

    public static Budget OpenJanuary(Money? limit = null) =>
        Budget.Create(Account, limit ?? TenThousandUsd, January).Value;

    public static BudgetPeriod Period(Budget budget) => budget.Period;

    public static ChargeKey Ec2(int day) =>
        ChargeKey.Create(new DateOnly(2026, 1, day), "AmazonEC2").Value;

    public static ChargeKey S3(int day) =>
        ChargeKey.Create(new DateOnly(2026, 1, day), "AmazonS3").Value;

    public static ChargeKey Ec2On(DateOnly usageDate) =>
        ChargeKey.Create(usageDate, "AmazonEC2").Value;
}
