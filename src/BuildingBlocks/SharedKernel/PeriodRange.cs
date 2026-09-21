namespace SharedKernel;

public sealed record PeriodRange
{
    public DateOnly Start { get; }

    public DateOnly End { get; }

    private PeriodRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static Result<PeriodRange> Create(DateOnly start, DateOnly end)
    {
        if (end <= start)
        {
            return Result.Failure<PeriodRange>(PeriodRangeErrors.InvalidRange);
        }

        return Result.Success(new PeriodRange(start, end));
    }

    public bool Contains(DateOnly date) => date >= Start && date < End;
}

public static class PeriodRangeErrors
{
    public static readonly Error InvalidRange = new(
        "PeriodRange.InvalidRange",
        "Period end must be after start.");
}
