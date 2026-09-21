using Budgeting.Domain;
using FluentAssertions;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

public class AccountIdTests
{
    [Fact]
    public void Should_ReturnInvalid_When_ValueIsEmpty()
    {
        Result<AccountId> result = AccountId.Create(string.Empty);

        result.Error.Should().Be(AccountIdErrors.Invalid);
    }

    [Fact]
    public void Should_ReturnInvalid_When_ValueHasElevenDigits()
    {
        Result<AccountId> result = AccountId.Create("12345678901");

        result.Error.Should().Be(AccountIdErrors.Invalid);
    }

    [Fact]
    public void Should_ReturnInvalid_When_ValueContainsLetters()
    {
        Result<AccountId> result = AccountId.Create("12345678901a");

        result.Error.Should().Be(AccountIdErrors.Invalid);
    }

    [Fact]
    public void Should_ReturnAccountId_When_ValueIsTwelveDigits()
    {
        const string value = "123456789012";

        AccountId accountId = AccountId.Create(value).Value;

        accountId.Value.Should().Be(value);
    }
}
