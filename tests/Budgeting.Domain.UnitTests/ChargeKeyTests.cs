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
}
