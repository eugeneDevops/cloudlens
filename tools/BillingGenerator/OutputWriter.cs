namespace BillingGenerator;

public static class OutputWriter
{
    public static void Write(GeneratorOptions options, GenerationResult result)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(result);

        string? directory = Path.GetDirectoryName(options.OutPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        CsvWriter.Write(options.OutPath, result.Rows);
        if (options.DuplicateBatch)
        {
            File.Copy(options.OutPath, CsvWriter.Batch2Path(options.OutPath), overwrite: true);
        }

        AnomalyJsonWriter.Write(AnomalyJsonWriter.PathBeside(options.OutPath), result.Catalog);
    }
}
