using System.Text.Json;

namespace BillingGenerator;

public static class AnomalyJsonWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        NewLine = "\n"
    };

    public static void Write(string path, AnomalyCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(catalog);

        byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(catalog, JsonOptions);
        File.WriteAllBytes(path, utf8);
    }

    public static string PathBeside(string csvPath)
    {
        string? directory = Path.GetDirectoryName(csvPath);
        return string.IsNullOrEmpty(directory)
            ? "anomalies.json"
            : Path.Combine(directory, "anomalies.json");
    }
}
