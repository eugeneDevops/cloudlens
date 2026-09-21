using SharedKernel;

namespace Budgeting.Domain;

public sealed class ThresholdState : Entity<Guid>
{
    public Threshold Threshold { get; private set; } = null!;

    public ThresholdStatus Status { get; private set; }

    internal ThresholdState(Threshold threshold)
        : base(Guid.NewGuid())
    {
        Threshold = threshold;
        Status = ThresholdStatus.Clear;
    }

    // Required by EF Core.
    private ThresholdState()
    {
    }

    internal void Latch() => Status = ThresholdStatus.Latched;

    internal void Reset() => Status = ThresholdStatus.Clear;
}
