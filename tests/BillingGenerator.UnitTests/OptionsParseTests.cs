using BillingGenerator;
using FluentAssertions;

namespace BillingGenerator.UnitTests;

public class OptionsParseTests
{
    [Fact]
    public void Should_ReturnError_When_ArgumentIsUnknown()
    {
        bool parsed = GeneratorOptions.TryParse(["--unknown"], out _, out string? error);

        (parsed, error).Should().Be((false, "Unknown argument '--unknown'."));
    }

    [Fact]
    public void Should_ReturnError_When_DaysIs29()
    {
        bool parsed = GeneratorOptions.TryParse(["--days", "29"], out _, out string? error);

        (parsed, error).Should().Be((false, "--days must be an integer >= 30."));
    }

    [Fact]
    public void Should_UseDefaultValues_When_ArgumentsAreMissing()
    {
        bool parsed = GeneratorOptions.TryParse([], out GeneratorOptions? options, out _);

        (parsed, options).Should().Be((
            true,
            new GeneratorOptions(
                GeneratorOptions.DefaultAccounts,
                GeneratorOptions.DefaultDays,
                GeneratorOptions.DefaultSeed,
                GeneratorOptions.DefaultOutPath,
                GeneratorOptions.DefaultStart,
                false,
                false,
                false)));
    }
}
