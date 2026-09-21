using SharedKernel;

namespace Budgeting.Domain;

public static class BudgetErrors
{
    public static readonly Error NonPositiveLimit = new(
        "Budget.NonPositiveLimit",
        "Budget limit must be greater than zero.");

    public static readonly Error UsageDateOutsidePeriod = new(
        "Budget.UsageDateOutsidePeriod",
        "Charge usage date does not fall within a budget period.");

    public static readonly Error CurrencyMismatch = new(
        "Budget.CurrencyMismatch",
        "Charge currency must match the budget limit currency.");

    public static readonly Error ClosedPeriodLimit = new(
        "Budget.ClosedPeriodLimit",
        "Limit cannot be changed on a closed period.");

    public static readonly Error AlreadyClosed = new(
        "Budget.AlreadyClosed",
        "Period is already closed.");
}
