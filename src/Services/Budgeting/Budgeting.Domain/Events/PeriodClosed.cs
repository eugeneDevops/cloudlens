using SharedKernel;

namespace Budgeting.Domain;

public sealed record PeriodClosed(
    Guid BudgetId,
    PeriodRange Range,
    Money Limit,
    Money Spent) : DomainEvent;
