using SharedKernel;

namespace Budgeting.Domain;

public sealed record LimitChanged(
    Guid BudgetId,
    Money PreviousLimit,
    Money NewLimit) : DomainEvent;
