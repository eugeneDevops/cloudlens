using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BillingGenerator;

public sealed record GeneratorOptions(
    int Accounts,
    int Days,
    int Seed,
    string OutPath,
    DateOnly Start,
    bool LateArrival,
    bool Corrections,
    bool DuplicateBatch)
{
    public const int DefaultAccounts = 3;
    public const int DefaultDays = 90;
    public const int DefaultSeed = 42;
    public const string DefaultOutPath = "out/billing.csv";
    public static readonly DateOnly DefaultStart = new(2026, 6, 1);

    public const string Usage =
        """
        Usage:
          BillingGenerator [--accounts <n>] [--days <n>] [--seed <n>] [--out <path>] [--start <yyyy-MM-dd>] [--late-arrival] [--corrections] [--duplicate-batch]

        Options:
          --accounts <n>        Number of accounts, 1-50 (default 3)
          --days <n>            Number of days, >= 30 (default 90)
          --seed <n>            Random seed (default 42)
          --out <path>          Output CSV path (default out/billing.csv)
          --start <yyyy-MM-dd>  First usage date (default 2026-06-01)
          --late-arrival        Delay ~5% of rows by 2-3 days
          --corrections         Emit ~2% restated rows
          --duplicate-batch     Also write <name>.batch2.csv
          --help                Show this help
        """;

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out GeneratorOptions? options,
        [NotNullWhen(false)] out string? error)
    {
        int accounts = DefaultAccounts;
        int days = DefaultDays;
        int seed = DefaultSeed;
        string outPath = DefaultOutPath;
        DateOnly start = DefaultStart;
        bool lateArrival = false;
        bool corrections = false;
        bool duplicateBatch = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg is "--late-arrival")
            {
                lateArrival = true;
                continue;
            }

            if (arg is "--corrections")
            {
                corrections = true;
                continue;
            }

            if (arg is "--duplicate-batch")
            {
                duplicateBatch = true;
                continue;
            }

            if (arg is not ("--accounts" or "--days" or "--seed" or "--out" or "--start"))
            {
                options = null;
                error = $"Unknown argument '{arg}'.";
                return false;
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                options = null;
                error = $"Missing value for {arg}.";
                return false;
            }

            i++;
            string value = args[i];
            switch (arg)
            {
                case "--accounts":
                    if (!TryParseInt(value, out accounts) || accounts < 1 || accounts > 50)
                    {
                        options = null;
                        error = "--accounts must be an integer between 1 and 50.";
                        return false;
                    }

                    break;
                case "--days":
                    if (!TryParseInt(value, out days) || days < 30)
                    {
                        options = null;
                        error = "--days must be an integer >= 30.";
                        return false;
                    }

                    break;
                case "--seed":
                    if (!TryParseInt(value, out seed))
                    {
                        options = null;
                        error = "--seed must be an integer.";
                        return false;
                    }

                    break;
                case "--out":
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        options = null;
                        error = "--out must be a non-empty path.";
                        return false;
                    }

                    outPath = value;
                    break;
                case "--start":
                    if (!DateOnly.TryParseExact(
                            value,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out start))
                    {
                        options = null;
                        error = "--start must be a date in yyyy-MM-dd format.";
                        return false;
                    }

                    break;
            }
        }

        options = new GeneratorOptions(
            accounts,
            days,
            seed,
            outPath,
            start,
            lateArrival,
            corrections,
            duplicateBatch);
        error = null;
        return true;
    }

    private static bool TryParseInt(string value, out int parsed)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }
}
