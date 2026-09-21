using SharedKernel;

namespace Budgeting.Domain;

public sealed class Budget : AggregateRoot<Guid>
{
    public BudgetPeriod Period { get; private set; } = null!;

    public static Result<Budget> Create(Money limit, PeriodRange period)
    {
        ArgumentNullException.ThrowIfNull(limit);
        ArgumentNullException.ThrowIfNull(period);

        if (limit.AmountMinor <= 0)
        {
            return Result.Failure<Budget>(BudgetErrors.NonPositiveLimit);
        }

        return Result.Success(new Budget(Guid.NewGuid(), limit, period));
    }

    private Budget(Guid id, Money limit, PeriodRange period)
        : base(id)
    {
        Period = BudgetPeriod.Open(limit, period);
    }

    // Required by EF Core.
    private Budget()
    {
    }

    public Result ApplyCharge(ChargeKey key, Money amount)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(amount);

        if (!Period.Range.Contains(key.UsageDate))
        {
            return Result.Failure(BudgetErrors.UsageDateOutsidePeriod);
        }

        return Period.ApplyCharge(key, amount, Id, Raise);
    }

    public Result ChangeLimit(Money newLimit)
    {
        ArgumentNullException.ThrowIfNull(newLimit);

        if (newLimit.AmountMinor <= 0)
        {
            return Result.Failure(BudgetErrors.NonPositiveLimit);
        }

        if (newLimit.Currency != Period.Limit.Currency)
        {
            return Result.Failure(BudgetErrors.CurrencyMismatch);
        }

        if (Period.IsClosed)
        {
            return Result.Failure(BudgetErrors.ClosedPeriodLimit);
        }

        Money previousLimit = Period.Limit;
        Period.ChangeLimit(newLimit);
        Raise(new LimitChanged(Id, previousLimit, newLimit));
        Period.EvaluateThresholds(Id, Raise);
        return Result.Success();
    }

    public Result ClosePeriod()
    {
        if (Period.IsClosed)
        {
            return Result.Failure(BudgetErrors.AlreadyClosed);
        }

        Period.Close();
        Raise(new PeriodClosed(Id, Period.Range, Period.Limit, Period.Spent));
        return Result.Success();
    }
}
