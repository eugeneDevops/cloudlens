namespace BillingGenerator;

public sealed record BillingRow(
    string LineItemId,
    DateOnly UsageStartDate,
    string AccountId,
    string Service,
    string ResourceId,
    string Tags,
    decimal UsageAmount,
    decimal UnblendedCost,
    string Currency,
    DateOnly ArrivalDate);
