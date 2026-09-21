using SharedKernel;

namespace Budgeting.Domain;

public sealed record ThresholdReset(
    Guid BudgetId,
    Threshold Threshold,
    Money Spent,
    Money Limit,
    decimal Utilization) : DomainEvent;
