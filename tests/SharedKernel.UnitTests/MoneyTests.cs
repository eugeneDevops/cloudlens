using FluentAssertions;

namespace SharedKernel.UnitTests;

public class MoneyTests
{
    [Fact]
    public void Should_ReturnFailure_When_AddingDifferentCurrencies()
    {
        Money usd = Money.Usd(100);
        Money eur = Money.Eur(50);

        Result<Money> result = usd.Add(eur);

        result.IsSuccess.Should().BeFalse();
    }
}
