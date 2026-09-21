using System.Globalization;

namespace BillingGenerator;

public static class Program
{
    public static int Main(string[] args)
    {
        if (ContainsHelp(args))
        {
            Console.Out.WriteLine(GeneratorOptions.Usage);
            return 0;
        }

        if (!GeneratorOptions.TryParse(args, out GeneratorOptions? options, out string? error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(GeneratorOptions.Usage);
            return 2;
        }

        GenerationResult result = Generator.Generate(options);
        OutputWriter.Write(options, result);

        Console.Out.WriteLine($"rows: {result.Rows.Count.ToString(CultureInfo.InvariantCulture)}");
        Console.Out.WriteLine($"total: {LastWriteWinsTotal(result.Rows).ToString(CultureInfo.InvariantCulture)}");
        Console.Out.WriteLine($"csv: {options.OutPath}");
        Console.Out.WriteLine($"anomalies: {AnomalyJsonWriter.PathBeside(options.OutPath)}");
        if (options.DuplicateBatch)
        {
            Console.Out.WriteLine($"batch2: {CsvWriter.Batch2Path(options.OutPath)}");
        }

        return 0;
    }

    private static bool ContainsHelp(string[] args)
    {
        foreach (string arg in args)
        {
            if (arg == "--help")
            {
                return true;
            }
        }

        return false;
    }

    private static decimal LastWriteWinsTotal(IReadOnlyList<BillingRow> rows)
    {
        Dictionary<string, decimal> last = new(StringComparer.Ordinal);
        foreach (BillingRow row in rows)
        {
            last[row.LineItemId] = row.UnblendedCost;
        }

        decimal total = 0m;
        foreach (decimal cost in last.Values)
        {
            total += cost;
        }

        return total;
    }
}
