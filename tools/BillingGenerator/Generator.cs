using System.Text;

namespace BillingGenerator;

public static class Generator
{
    private const string Currency = "USD";
    private const string ResourceId = "";
    private const string Tags = "env=prod";

    private static readonly string[] ServiceNames =
    [
        "AmazonEC2",
        "AmazonS3",
        "AmazonRDS",
        "AWSLambda",
        "AmazonDynamoDB",
        "AmazonCloudFront",
        "AmazonEKS",
        "AmazonSQS",
        "AmazonCloudWatch",
        "AWSDataTransfer"
    ];

    private const int LateArrivalSeedXor = 0x4C415445;
    private const int CorrectionsSeedXor = 0x434F5252;
    private const string LineItemAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    private static readonly Dictionary<string, decimal> UnitPrices = new(StringComparer.Ordinal)
    {
        ["AmazonEC2"] = 0.10m,
        ["AmazonS3"] = 0.023m,
        ["AmazonRDS"] = 0.17m,
        ["AWSLambda"] = 0.20m,
        ["AmazonDynamoDB"] = 0.25m,
        ["AmazonCloudFront"] = 0.085m,
        ["AmazonEKS"] = 0.10m,
        ["AmazonSQS"] = 0.40m,
        ["AmazonCloudWatch"] = 0.30m,
        ["AWSDataTransfer"] = 0.09m
    };

    public static GenerationResult Generate(GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Random random = new(options.Seed);
        DateOnly lastDay = options.Start.AddDays(options.Days - 1);

        List<AccountPlan> accounts = CreateAccounts(options, random);
        AnomalyRecord[] anomalies = CreateAnomalies(options, accounts, lastDay, random);
        AnomalyCatalog catalog = new(options.Seed, options.Start, options.Days, anomalies);

        Dictionary<(string AccountId, string Service), AnomalyRecord> anomalyByKey =
            anomalies.ToDictionary(a => (a.AccountId, a.Service));

        List<BillingRow> rows = CreateBaseRows(options, accounts, lastDay, anomalyByKey, random);

        if (options.LateArrival)
        {
            ApplyLateArrival(rows, options.Seed);
        }

        List<BillingRow> emitted = options.Corrections
            ? ApplyCorrections(rows, options.Seed)
            : rows;

        emitted.Sort(CompareOutputOrder);
        return new GenerationResult(emitted, catalog);
    }

    private static List<AccountPlan> CreateAccounts(GeneratorOptions options, Random random)
    {
        HashSet<string> usedIds = new(StringComparer.Ordinal);
        List<AccountPlan> accounts = new(options.Accounts);

        for (int i = 0; i < options.Accounts; i++)
        {
            string accountId;
            do
            {
                accountId = CreateAccountId(random);
            }
            while (!usedIds.Add(accountId));

            int serviceCount = random.Next(6, 11);
            List<string> pool = [.. ServiceNames];
            Shuffle(pool, random);
            List<string> selected = pool.GetRange(0, serviceCount);
            selected.Sort(StringComparer.Ordinal);

            List<ServicePlan> services = new(selected.Count);
            foreach (string name in selected)
            {
                decimal baseDailyCost = LogUniform(random, 1m, 500m);
                services.Add(new ServicePlan(name, baseDailyCost));
            }

            accounts.Add(new AccountPlan(accountId, services));
        }

        return accounts;
    }

    private static AnomalyRecord[] CreateAnomalies(
        GeneratorOptions options,
        IReadOnlyList<AccountPlan> accounts,
        DateOnly lastDay,
        Random random)
    {
        List<AnomalyRecord> anomalies = [];
        DateOnly spikeMin = options.Start.AddDays(13);
        DateOnly stepMin = options.Start.AddDays(14);
        DateOnly stepMax = lastDay.AddDays(-13);
        DateOnly driftMin = options.Start.AddDays(13);
        DateOnly driftMax = lastDay.AddDays(-6);

        foreach (AccountPlan account in accounts)
        {
            List<ServicePlan> shuffled = [.. account.Services];
            Shuffle(shuffled, random);

            ServicePlan spikeService = shuffled[0];
            ServicePlan stepService = shuffled[1];
            ServicePlan driftService = shuffled[2];

            DateOnly spikeDay = PickSpikeDate(random, options.Start, spikeMin, lastDay);
            decimal spikeMultiplier = Round(NextDecimal(random, 3m, 5m));
            anomalies.Add(new AnomalyRecord(
                "spike",
                account.AccountId,
                spikeService.Name,
                spikeDay,
                spikeDay,
                spikeMultiplier,
                spikeService.BaseDailyCost));

            DateOnly stepDay = PickDate(random, stepMin, stepMax);
            anomalies.Add(new AnomalyRecord(
                "step",
                account.AccountId,
                stepService.Name,
                stepDay,
                lastDay,
                2.0m,
                stepService.BaseDailyCost));

            DateOnly driftDay = PickDate(random, driftMin, driftMax);
            anomalies.Add(new AnomalyRecord(
                "drift",
                account.AccountId,
                driftService.Name,
                driftDay,
                driftDay.AddDays(6),
                0.08m,
                driftService.BaseDailyCost));
        }

        return [.. anomalies
            .OrderBy(a => a.AccountId, StringComparer.Ordinal)
            .ThenBy(a => a.Type, StringComparer.Ordinal)];
    }

    private static List<BillingRow> CreateBaseRows(
        GeneratorOptions options,
        IReadOnlyList<AccountPlan> accounts,
        DateOnly lastDay,
        IReadOnlyDictionary<(string AccountId, string Service), AnomalyRecord> anomalies,
        Random random)
    {
        List<BillingRow> rows = [];
        HashSet<string> lineItemIds = new(StringComparer.Ordinal);

        for (DateOnly day = options.Start; day <= lastDay; day = day.AddDays(1))
        {
            decimal weekly = IsWeekend(day) ? 0.7m : 1.0m;
            foreach (AccountPlan account in accounts)
            {
                foreach (ServicePlan service in account.Services)
                {
                    decimal noise = NextDecimal(random, 0.95m, 1.05m);
                    decimal anomalyMultiplier = AppliedMultiplier(
                        anomalies.GetValueOrDefault((account.AccountId, service.Name)),
                        day);
                    decimal cost = Round(service.BaseDailyCost * weekly * noise * anomalyMultiplier);
                    decimal usage = Round(cost / UnitPrices[service.Name]);
                    string lineItemId = CreateLineItemId(random, lineItemIds);
                    rows.Add(new BillingRow(
                        lineItemId,
                        day,
                        account.AccountId,
                        service.Name,
                        ResourceId,
                        Tags,
                        usage,
                        cost,
                        Currency,
                        day));
                }
            }
        }

        return rows;
    }

    private static void ApplyLateArrival(List<BillingRow> rows, int seed)
    {
        Random random = new(seed ^ LateArrivalSeedXor);
        for (int i = 0; i < rows.Count; i++)
        {
            if (random.NextDouble() >= 0.05)
            {
                continue;
            }

            int delay = random.Next(2, 4);
            BillingRow row = rows[i];
            rows[i] = row with { ArrivalDate = row.ArrivalDate.AddDays(delay) };
        }
    }

    private static List<BillingRow> ApplyCorrections(IReadOnlyList<BillingRow> rows, int seed)
    {
        Random random = new(seed ^ CorrectionsSeedXor);
        List<BillingRow> emitted = [];

        foreach (BillingRow row in rows)
        {
            if (random.NextDouble() >= 0.02)
            {
                emitted.Add(row);
                continue;
            }

            decimal factor;
            do
            {
                factor = NextDecimal(random, 0.85m, 1.15m);
            }
            while (factor is >= 0.99m and <= 1.01m);

            decimal wrongCost = Round(row.UnblendedCost * factor);
            decimal wrongUsage = Round(wrongCost / UnitPrices[row.Service]);
            int delay = random.Next(1, 4);

            emitted.Add(row with { UsageAmount = wrongUsage, UnblendedCost = wrongCost });
            emitted.Add(row with { ArrivalDate = row.ArrivalDate.AddDays(delay) });
        }

        return emitted;
    }

    private static decimal AppliedMultiplier(AnomalyRecord? anomaly, DateOnly date)
    {
        if (anomaly is null)
        {
            return 1.0m;
        }

        if (anomaly.Type == "spike")
        {
            return date == anomaly.StartDate ? anomaly.Multiplier : 1.0m;
        }

        if (anomaly.Type == "step")
        {
            return date >= anomaly.StartDate ? anomaly.Multiplier : 1.0m;
        }

        if (date < anomaly.StartDate)
        {
            return 1.0m;
        }

        int dayIndex = date.DayNumber - anomaly.StartDate.DayNumber + 1;
        int exponent = dayIndex <= 7 ? dayIndex : 7;
        return Pow108(exponent);
    }

    private static DateOnly PickSpikeDate(Random random, DateOnly start, DateOnly min, DateOnly lastDay)
    {
        List<DateOnly> candidates = [];
        for (DateOnly day = min; day <= lastDay; day = day.AddDays(1))
        {
            if (IsWeekend(day))
            {
                continue;
            }

            if (CountPriorSameTypeDays(start, day) >= 7)
            {
                candidates.Add(day);
            }
        }

        return candidates[random.Next(candidates.Count)];
    }

    private static int CountPriorSameTypeDays(DateOnly start, DateOnly day)
    {
        bool weekend = IsWeekend(day);
        int count = 0;
        for (DateOnly cursor = day.AddDays(-1); cursor >= start; cursor = cursor.AddDays(-1))
        {
            if (IsWeekend(cursor) == weekend)
            {
                count++;
            }
        }

        return count;
    }

    private static DateOnly PickDate(Random random, DateOnly min, DateOnly max)
    {
        int span = max.DayNumber - min.DayNumber;
        return min.AddDays(random.Next(span + 1));
    }

    private static string CreateAccountId(Random random)
    {
        Span<char> chars = stackalloc char[12];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('0' + random.Next(10));
        }

        return new string(chars);
    }

    private static string CreateLineItemId(Random random, HashSet<string> used)
    {
        StringBuilder builder = new(26);
        while (true)
        {
            builder.Clear();
            for (int i = 0; i < 26; i++)
            {
                builder.Append(LineItemAlphabet[random.Next(LineItemAlphabet.Length)]);
            }

            string id = builder.ToString();
            if (used.Add(id))
            {
                return id;
            }
        }
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static decimal LogUniform(Random random, decimal min, decimal max)
    {
        double logMin = Math.Log((double)min);
        double logMax = Math.Log((double)max);
        double sample = Math.Exp(logMin + (random.NextDouble() * (logMax - logMin)));
        return Round((decimal)sample);
    }

    private static decimal NextDecimal(Random random, decimal min, decimal max)
    {
        return min + ((max - min) * (decimal)random.NextDouble());
    }

    private static decimal Round(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal Pow108(int exponent)
    {
        decimal value = 1m;
        for (int i = 0; i < exponent; i++)
        {
            value *= 1.08m;
        }

        return value;
    }

    private static bool IsWeekend(DateOnly day)
    {
        return day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
    }

    private static int CompareOutputOrder(BillingRow left, BillingRow right)
    {
        int arrival = left.ArrivalDate.CompareTo(right.ArrivalDate);
        if (arrival != 0)
        {
            return arrival;
        }

        int account = string.CompareOrdinal(left.AccountId, right.AccountId);
        if (account != 0)
        {
            return account;
        }

        int service = string.CompareOrdinal(left.Service, right.Service);
        if (service != 0)
        {
            return service;
        }

        int usage = left.UsageStartDate.CompareTo(right.UsageStartDate);
        if (usage != 0)
        {
            return usage;
        }

        return string.CompareOrdinal(left.LineItemId, right.LineItemId);
    }

    private sealed record AccountPlan(string AccountId, IReadOnlyList<ServicePlan> Services);

    private sealed record ServicePlan(string Name, decimal BaseDailyCost);
}
