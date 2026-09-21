namespace Budgeting.Domain;

public sealed record Threshold
{
    public static readonly Threshold Fifty = new(50);

    public static readonly Threshold Eighty = new(80);

    public static readonly Threshold Hundred = new(100);

    public int Percent { get; }

    private Threshold(int percent)
    {
        Percent = percent;
    }
}
