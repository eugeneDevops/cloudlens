using System.Globalization;
using System.Text.Json;
using BillingGenerator;

namespace BillingGenerator.UnitTests;

internal static class BillingFiles
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static GeneratorOptions Options(
        string csvPath,
        int seed = GeneratorOptions.DefaultSeed,
        bool lateArrival = false,
        bool corrections = false,
        bool duplicateBatch = false)
    {
        return new GeneratorOptions(
            GeneratorOptions.DefaultAccounts,
            GeneratorOptions.DefaultDays,
            seed,
            csvPath,
            GeneratorOptions.DefaultStart,
            lateArrival,
            corrections,
            duplicateBatch);
    }

    public static void Write(GeneratorOptions options)
    {
        OutputWriter.Write(options, Generator.Generate(options));
    }

    public static IReadOnlyList<CsvLine> ReadCsv(string path)
    {
        string text = File.ReadAllText(path);
        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        List<CsvLine> rows = [];
        for (int i = 1; i < lines.Length; i++)
        {
            string[] fields = lines[i].Split(',');
            rows.Add(new CsvLine(
                fields[0],
                DateOnly.ParseExact(fields[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                fields[2],
                fields[3],
                fields[4],
                fields[5],
                decimal.Parse(fields[6], CultureInfo.InvariantCulture),
                decimal.Parse(fields[7], CultureInfo.InvariantCulture),
                fields[8]));
        }

        return rows;
    }

    public static AnomalyCatalog ReadCatalog(string csvPath)
    {
        string json = File.ReadAllText(AnomalyJsonWriter.PathBeside(csvPath));
        AnomalyCatalog? catalog = JsonSerializer.Deserialize<AnomalyCatalog>(json, JsonOptions);
        return catalog ?? throw new InvalidOperationException("anomalies.json deserialized to null.");
    }

    public static Dictionary<(string AccountId, DateOnly Date, string Service), decimal> CostByGrain(
        IEnumerable<CsvLine> lines)
    {
        return lines
            .GroupBy(line => (line.AccountId, line.UsageStartDate, line.Service))
            .ToDictionary(group => group.Key, group => group.Sum(line => line.UnblendedCost));
    }

    public static IReadOnlyList<CsvLine> LastWriteWins(IEnumerable<CsvLine> lines)
    {
        Dictionary<string, CsvLine> last = new(StringComparer.Ordinal);
        foreach (CsvLine line in lines)
        {
            last[line.LineItemId] = line;
        }

        return last.Values.ToList();
    }

    public static bool IsWeekend(DateOnly day)
    {
        return day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }
}

internal sealed record CsvLine(
    string LineItemId,
    DateOnly UsageStartDate,
    string AccountId,
    string Service,
    string ResourceId,
    string Tags,
    decimal UsageAmount,
    decimal UnblendedCost,
    string Currency);

internal sealed class TempCsv : IDisposable
{
    public TempCsv()
    {
        Directory = Path.Combine(Path.GetTempPath(), "BillingGeneratorTests", Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        CsvPath = Path.Combine(Directory, "billing.csv");
    }

    public string Directory { get; }

    public string CsvPath { get; }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
