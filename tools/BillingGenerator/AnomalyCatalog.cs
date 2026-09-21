namespace BillingGenerator;

public sealed record AnomalyCatalog(
    int Seed,
    DateOnly Start,
    int Days,
    AnomalyRecord[] Anomalies);

public sealed record AnomalyRecord(
    string Type,
    string AccountId,
    string Service,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Multiplier,
    decimal BaseDailyCost);
