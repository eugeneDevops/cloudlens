using Budgeting.Domain;
using FluentAssertions;
using SharedKernel;

namespace Budgeting.Domain.UnitTests;

public class ChargeKeyTests
{
    [Fact]
    public void Should_ReturnEmptyService_When_ServiceIsWhitespace()
    {
        Result<ChargeKey> result = ChargeKey.Create(new DateOnly(2026, 1, 10), "   ");

        result.Error.Should().Be(ChargeKeyErrors.EmptyService);
    }

    [Fact]
    public void Should_BeEqual_When_ServicesDifferByPaddingAndCase()
    {
        DateOnly usageDate = new(2026, 1, 10);

        ChargeKey padded = ChargeKey.Create(usageDate, " AmazonEC2 ").Value;
        ChargeKey lower = ChargeKey.Create(usageDate, "amazonec2").Value;

        padded.Should().Be(lower);
    }
}
