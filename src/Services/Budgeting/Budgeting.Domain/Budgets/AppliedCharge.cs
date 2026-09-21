using SharedKernel;

namespace Budgeting.Domain;

public sealed class AppliedCharge : Entity<ChargeKey>
{
    public ChargeKey Key => Id;

    public Money Amount { get; private set; } = null!;

    internal AppliedCharge(ChargeKey key, Money amount)
        : base(key)
    {
        Amount = amount;
    }

    // Required by EF Core.
    private AppliedCharge()
    {
    }

    internal void ReplaceAmount(Money amount) => Amount = amount;
}
