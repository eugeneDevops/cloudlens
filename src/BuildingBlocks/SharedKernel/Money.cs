namespace SharedKernel;

public sealed record Money
{
    public string Currency { get; }

    public long AmountMinor { get; }

    private Money(string currency, long amountMinor)
    {
        Currency = currency;
        AmountMinor = amountMinor;
    }

    public static Money Usd(long amountMinor) => new("USD", amountMinor);

    public static Money Eur(long amountMinor) => new("EUR", amountMinor);

    public Result<Money> Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            return Result.Failure<Money>(MoneyErrors.CurrencyMismatch);
        }

        return Result.Success(new Money(Currency, checked(AmountMinor + other.AmountMinor)));
    }

    public Result<Money> Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            return Result.Failure<Money>(MoneyErrors.CurrencyMismatch);
        }

        return Result.Success(new Money(Currency, checked(AmountMinor - other.AmountMinor)));
    }

    public int CompareTo(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies.");
        }

        return AmountMinor.CompareTo(other.AmountMinor);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
}

public static class MoneyErrors
{
    public static readonly Error CurrencyMismatch = new(
        "Money.CurrencyMismatch",
        "Money operations require the same currency.");
}
