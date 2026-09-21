using BillingGenerator;
using FluentAssertions;

namespace BillingGenerator.UnitTests;

public class AnomalyTests
{
    [Fact]
    public void Should_HaveSpikeCostAtLeastTwoAndAHalfTimesSameTypeMedian_When_Generated()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath));
        AnomalyCatalog catalog = BillingFiles.ReadCatalog(output.CsvPath);
        IReadOnlyList<CsvLine> lines = BillingFiles.ReadCsv(output.CsvPath);
        Dictionary<(string AccountId, DateOnly Date, string Service), decimal> costs = BillingFiles.CostByGrain(lines);

        IEnumerable<decimal> ratios = catalog.Anomalies
            .Where(anomaly => anomaly.Type == "spike")
            .Select(anomaly => SpikeRatio(anomaly, costs, catalog.Start));

        ratios.Should().OnlyContain(ratio => ratio >= 2.5m);
    }

    [Fact]
    public void Should_HaveStepAverageAtLeast1_8TimesPreStepAverage_When_Generated()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath));
        AnomalyCatalog catalog = BillingFiles.ReadCatalog(output.CsvPath);
        IReadOnlyList<CsvLine> lines = BillingFiles.ReadCsv(output.CsvPath);
        Dictionary<(string AccountId, DateOnly Date, string Service), decimal> costs = BillingFiles.CostByGrain(lines);

        IEnumerable<decimal> ratios = catalog.Anomalies
            .Where(anomaly => anomaly.Type == "step")
            .Select(anomaly => StepRatio(anomaly, costs));

        ratios.Should().OnlyContain(ratio => ratio >= 1.8m);
    }

    [Fact]
    public void Should_AssignThreeAnomalyTypesOnDistinctServicesInsideWindow_When_Generated()
    {
        using TempCsv output = new();
        BillingFiles.Write(BillingFiles.Options(output.CsvPath));
        AnomalyCatalog catalog = BillingFiles.ReadCatalog(output.CsvPath);
        DateOnly windowStart = catalog.Start.AddDays(13);
        DateOnly lastDay = catalog.Start.AddDays(catalog.Days - 1);

        IEnumerable<IGrouping<string, AnomalyRecord>> byAccount = catalog.Anomalies.GroupBy(anomaly => anomaly.AccountId);

        byAccount.Should().OnlyContain(group => IsCompleteAccountWindow(group, windowStart, lastDay));
    }

    private static decimal SpikeRatio(
        AnomalyRecord spike,
        IReadOnlyDictionary<(string AccountId, DateOnly Date, string Service), decimal> costs,
        DateOnly rangeStart)
    {
        decimal spikeCost = costs[(spike.AccountId, spike.StartDate, spike.Service)];
        bool weekend = BillingFiles.IsWeekend(spike.StartDate);
        List<decimal> prior = [];
        for (DateOnly day = spike.StartDate.AddDays(-1); prior.Count < 7; day = day.AddDays(-1))
        {
            if (day < rangeStart)
            {
                break;
            }

            if (BillingFiles.IsWeekend(day) == weekend)
            {
                prior.Add(costs[(spike.AccountId, day, spike.Service)]);
            }
        }

        prior.Sort();
        return spikeCost / prior[3];
    }

    private static decimal StepRatio(
        AnomalyRecord step,
        IReadOnlyDictionary<(string AccountId, DateOnly Date, string Service), decimal> costs)
    {
        decimal before = AverageCost(step, costs, step.StartDate.AddDays(-14), step.StartDate.AddDays(-1));
        decimal after = AverageCost(step, costs, step.StartDate, step.StartDate.AddDays(13));
        return after / before;
    }

    private static decimal AverageCost(
        AnomalyRecord step,
        IReadOnlyDictionary<(string AccountId, DateOnly Date, string Service), decimal> costs,
        DateOnly from,
        DateOnly to)
    {
        decimal sum = 0m;
        int count = 0;
        for (DateOnly day = from; day <= to; day = day.AddDays(1))
        {
            sum += costs[(step.AccountId, day, step.Service)];
            count++;
        }

        return sum / count;
    }

    private static bool IsCompleteAccountWindow(
        IGrouping<string, AnomalyRecord> group,
        DateOnly windowStart,
        DateOnly lastDay)
    {
        AnomalyRecord[] anomalies = [.. group];
        if (anomalies.Length != 3)
        {
            return false;
        }

        string[] types = [.. anomalies.Select(anomaly => anomaly.Type).Order(StringComparer.Ordinal)];
        if (types is not ["drift", "spike", "step"])
        {
            return false;
        }

        if (anomalies.Select(anomaly => anomaly.Service).Distinct(StringComparer.Ordinal).Count() != 3)
        {
            return false;
        }

        foreach (AnomalyRecord anomaly in anomalies)
        {
            if (anomaly.StartDate < windowStart || anomaly.EndDate > lastDay)
            {
                return false;
            }

            if (anomaly.Type == "spike" && anomaly.StartDate != anomaly.EndDate)
            {
                return false;
            }

            if (anomaly.Type == "step" && anomaly.EndDate != lastDay)
            {
                return false;
            }

            if (anomaly.Type == "drift" && anomaly.EndDate != anomaly.StartDate.AddDays(6))
            {
                return false;
            }
        }

        return true;
    }
}
