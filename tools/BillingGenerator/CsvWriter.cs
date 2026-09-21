using System.Globalization;
using System.Text;

namespace BillingGenerator;

public static class CsvWriter
{
    public const string Header =
        "line_item_id,usage_start_date,account_id,service,resource_id,tags,usage_amount,unblended_cost,currency";

    public static void Write(string path, IReadOnlyList<BillingRow> rows)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(rows);

        using FileStream stream = File.Create(path);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            NewLine = "\n"
        };

        writer.WriteLine(Header);
        foreach (BillingRow row in rows)
        {
            writer.Write(Escape(row.LineItemId));
            writer.Write(',');
            writer.Write(row.UsageStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(Escape(row.AccountId));
            writer.Write(',');
            writer.Write(Escape(row.Service));
            writer.Write(',');
            writer.Write(Escape(row.ResourceId));
            writer.Write(',');
            writer.Write(Escape(row.Tags));
            writer.Write(',');
            writer.Write(row.UsageAmount.ToString(CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(row.UnblendedCost.ToString(CultureInfo.InvariantCulture));
            writer.Write(',');
            writer.Write(Escape(row.Currency));
            writer.Write('\n');
        }
    }

    public static string Batch2Path(string csvPath)
    {
        string? directory = Path.GetDirectoryName(csvPath);
        string fileName = Path.GetFileNameWithoutExtension(csvPath) + ".batch2.csv";
        return string.IsNullOrEmpty(directory) ? fileName : Path.Combine(directory, fileName);
    }

    private static string Escape(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return field;
    }
}
