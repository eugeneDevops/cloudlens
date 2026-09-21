namespace BillingGenerator;

public sealed record GenerationResult(
    IReadOnlyList<BillingRow> Rows,
    AnomalyCatalog Catalog);
