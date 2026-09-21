using SharedKernel;

namespace Budgeting.Domain;

public sealed record AccountId
{
    public string Value { get; }

    private AccountId(string value)
    {
        Value = value;
    }

    public static Result<AccountId> Create(string? value)
    {
        if (value is not { Length: 12 } || !value.All(char.IsAsciiDigit))
        {
            return Result.Failure<AccountId>(AccountIdErrors.Invalid);
        }

        return Result.Success(new AccountId(value));
    }
}

public static class AccountIdErrors
{
    public static readonly Error Invalid = new(
        "AccountId.Invalid",
        "Account id must be exactly 12 digits.");
}
