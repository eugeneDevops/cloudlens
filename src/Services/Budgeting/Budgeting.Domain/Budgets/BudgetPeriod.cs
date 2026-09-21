using SharedKernel;

namespace Budgeting.Domain;

public sealed class BudgetPeriod : Entity<Guid>
{
    private const int HysteresisPercentagePoints = 5;

    private readonly List<AppliedCharge> _appliedCharges = [];

    private readonly List<ThresholdState> _thresholds = [];

    public PeriodRange Range { get; private set; } = null!;

    public Money Limit { get; private set; } = null!;

    public Money Spent { get; private set; } = null!;

    public bool IsClosed { get; private set; }

    public IReadOnlyCollection<AppliedCharge> AppliedCharges => _appliedCharges;

    public IReadOnlyCollection<ThresholdState> Thresholds => _thresholds;

    internal static BudgetPeriod Open(Money limit, PeriodRange range) =>
        new(Guid.NewGuid(), limit, range);

    private BudgetPeriod(Guid id, Money limit, PeriodRange range)
        : base(id)
    {
        Range = range;
        Limit = limit;
        Spent = limit.Subtract(limit).Value;
        IsClosed = false;
        _thresholds.Add(new ThresholdState(Threshold.Fifty));
        _thresholds.Add(new ThresholdState(Threshold.Eighty));
        _thresholds.Add(new ThresholdState(Threshold.Hundred));
    }

    // Required by EF Core.
    private BudgetPeriod()
    {
    }

    internal Result ApplyCharge(ChargeKey key, Money amount, Guid budgetId, Action<IDomainEvent> raise)
    {
        if (amount.Currency != Limit.Currency)
        {
            return Result.Failure(BudgetErrors.CurrencyMismatch);
        }

        AppliedCharge? existing = _appliedCharges.SingleOrDefault(charge => charge.Key == key);
        if (existing is null)
        {
            _appliedCharges.Add(new AppliedCharge(key, amount));
            Spent = Spent.Add(amount).Value;
            EvaluateThresholds(budgetId, raise);
            return Result.Success();
        }

        if (existing.Amount == amount)
        {
            return Result.Success();
        }

        Money delta = amount.Subtract(existing.Amount).Value;
        Spent = Spent.Add(delta).Value;
        existing.ReplaceAmount(amount);
        EvaluateThresholds(budgetId, raise);
        return Result.Success();
    }

    internal void ChangeLimit(Money newLimit) => Limit = newLimit;

    internal void Close() => IsClosed = true;

    internal void EvaluateThresholds(Guid budgetId, Action<IDomainEvent> raise)
    {
        decimal utilization = UtilizationPercent();

        foreach (ThresholdState state in _thresholds.OrderBy(item => item.Threshold.Percent))
        {
            decimal threshold = state.Threshold.Percent;
            if (state.Status == ThresholdStatus.Clear && utilization >= threshold)
            {
                state.Latch();
                raise(new ThresholdAlertRaised(budgetId, state.Threshold, Spent, Limit, utilization));
            }
            else if (state.Status == ThresholdStatus.Latched
                     && utilization <= threshold - HysteresisPercentagePoints)
            {
                state.Reset();
                raise(new ThresholdReset(budgetId, state.Threshold, Spent, Limit, utilization));
            }
        }
    }

    private decimal UtilizationPercent() =>
        (decimal)Spent.AmountMinor * 100m / Limit.AmountMinor;
}
