using FluentAssertions;

namespace SharedKernel.UnitTests;

public class PeriodRangeTests
{
    [Fact]
    public void Should_ContainDate_When_DateEqualsStart()
    {
        PeriodRange range = PeriodRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1)).Value;

        bool contains = range.Contains(new DateOnly(2026, 1, 1));

        contains.Should().BeTrue();
    }

    [Fact]
    public void Should_NotContainDate_When_DateEqualsEnd()
    {
        PeriodRange range = PeriodRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1)).Value;

        bool contains = range.Contains(new DateOnly(2026, 2, 1));

        contains.Should().BeFalse();
    }

    [Fact]
    public void Should_ContainDate_When_DateIsLastDayOfRange()
    {
        PeriodRange range = PeriodRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1)).Value;

        bool contains = range.Contains(new DateOnly(2026, 1, 31));

        contains.Should().BeTrue();
    }

    [Fact]
    public void Should_NotContainDate_When_DateIsBeforeStart()
    {
        PeriodRange range = PeriodRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1)).Value;

        bool contains = range.Contains(new DateOnly(2025, 12, 31));

        contains.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_ReturnFailure_When_EndIsNotAfterStart(int endOffsetDays)
    {
        DateOnly start = new(2026, 1, 1);

        Result<PeriodRange> result = PeriodRange.Create(start, start.AddDays(endOffsetDays));

        result.Error.Should().Be(PeriodRangeErrors.InvalidRange);
    }
}
