using BillingGenerator;
using FluentAssertions;

namespace BillingGenerator.UnitTests;

public class ModeTests
{
    [Fact]
    public void Should_MatchBaseCostsAfterLastLineItemWins_When_CorrectionsEnabled()
    {
        using TempCsv baseline = new();
        using TempCsv corrected = new();
        BillingFiles.Write(BillingFiles.Options(baseline.CsvPath));
        BillingFiles.Write(BillingFiles.Options(corrected.CsvPath, corrections: true));

        BillingFiles.CostByGrain(BillingFiles.LastWriteWins(BillingFiles.ReadCsv(corrected.CsvPath)))
            .Should()
            .BeEquivalentTo(BillingFiles.CostByGrain(BillingFiles.ReadCsv(baseline.CsvPath)));
    }

    [Fact]
    public void Should_ContainDuplicateLineItemIds_When_CorrectionsEnabled()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath, corrections: true));

        BillingFiles.ReadCsv(output.CsvPath)
            .GroupBy(line => line.LineItemId, StringComparer.Ordinal)
            .Should()
            .Contain(group => group.Count() > 1);
    }

    [Fact]
    public void Should_PreserveCsvRowMultiset_When_LateArrivalEnabled()
    {
        using TempCsv baseline = new();
        using TempCsv delayed = new();
        BillingFiles.Write(BillingFiles.Options(baseline.CsvPath));
        BillingFiles.Write(BillingFiles.Options(delayed.CsvPath, lateArrival: true));

        BillingFiles.ReadCsv(delayed.CsvPath)
            .OrderBy(line => line.LineItemId, StringComparer.Ordinal)
            .Should()
            .Equal(BillingFiles.ReadCsv(baseline.CsvPath).OrderBy(line => line.LineItemId, StringComparer.Ordinal));
    }

    [Fact]
    public void Should_ChangeCsvOrder_When_LateArrivalEnabled()
    {
        using TempCsv baseline = new();
        using TempCsv delayed = new();
        BillingFiles.Write(BillingFiles.Options(baseline.CsvPath));
        BillingFiles.Write(BillingFiles.Options(delayed.CsvPath, lateArrival: true));

        BillingFiles.ReadCsv(delayed.CsvPath)
            .Select(line => line.LineItemId)
            .Should()
            .NotEqual(BillingFiles.ReadCsv(baseline.CsvPath).Select(line => line.LineItemId));
    }

    [Fact]
    public void Should_ContainUsageDateBeforePreviousRow_When_LateArrivalEnabled()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath, lateArrival: true));
        IReadOnlyList<CsvLine> lines = BillingFiles.ReadCsv(output.CsvPath);

        bool inversion = false;
        for (int i = 1; i < lines.Count; i++)
        {
            if (lines[i].UsageStartDate < lines[i - 1].UsageStartDate)
            {
                inversion = true;
                break;
            }
        }

        inversion.Should().BeTrue();
    }

    [Fact]
    public void Should_WriteByteIdenticalBatch2Csv_When_DuplicateBatchEnabled()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath, duplicateBatch: true));

        File.ReadAllBytes(CsvWriter.Batch2Path(output.CsvPath))
            .Should()
            .Equal(File.ReadAllBytes(output.CsvPath));
    }
}
