using SharedKernel;

namespace Budgeting.Domain;

public sealed record ChargeKey
{
    public DateOnly UsageDate { get; }

    public string Service { get; }

    private ChargeKey(DateOnly usageDate, string service)
    {
        UsageDate = usageDate;
        Service = service;
    }

    public static Result<ChargeKey> Create(DateOnly usageDate, string? service)
    {
        if (string.IsNullOrWhiteSpace(service))
        {
            return Result.Failure<ChargeKey>(ChargeKeyErrors.EmptyService);
        }

        // Normalization is required: "AmazonEC2" and "amazonec2" would otherwise become two grains and double-count.
        return Result.Success(new ChargeKey(usageDate, service.Trim().ToLowerInvariant()));
    }
}

public static class ChargeKeyErrors
{
    public static readonly Error EmptyService = new(
        "ChargeKey.EmptyService",
        "Service must not be empty.");
}
