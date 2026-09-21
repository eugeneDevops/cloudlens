using BillingGenerator;
using FluentAssertions;

namespace BillingGenerator.UnitTests;

public class DeterminismTests
{
    [Fact]
    public void Should_ProduceByteIdenticalCsv_When_SeedIsTheSame()
    {
        using TempCsv first = new();
        using TempCsv second = new();
        GeneratorOptions options = BillingFiles.Options(first.CsvPath);
        BillingFiles.Write(options);
        BillingFiles.Write(options with { OutPath = second.CsvPath });

        File.ReadAllBytes(second.CsvPath).Should().Equal(File.ReadAllBytes(first.CsvPath));
    }

    [Fact]
    public void Should_ProduceByteIdenticalAnomaliesJson_When_SeedIsTheSame()
    {
        using TempCsv first = new();
        using TempCsv second = new();
        GeneratorOptions options = BillingFiles.Options(first.CsvPath);
        BillingFiles.Write(options);
        BillingFiles.Write(options with { OutPath = second.CsvPath });

        File.ReadAllBytes(AnomalyJsonWriter.PathBeside(second.CsvPath))
            .Should()
            .Equal(File.ReadAllBytes(AnomalyJsonWriter.PathBeside(first.CsvPath)));
    }

    [Fact]
    public void Should_ProduceDifferentCsv_When_SeedDiffers()
    {
        using TempCsv first = new();
        using TempCsv second = new();
        BillingFiles.Write(BillingFiles.Options(first.CsvPath, seed: 42));
        BillingFiles.Write(BillingFiles.Options(second.CsvPath, seed: 43));

        File.ReadAllBytes(second.CsvPath).Should().NotEqual(File.ReadAllBytes(first.CsvPath));
    }
}
