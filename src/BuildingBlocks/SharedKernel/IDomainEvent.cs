namespace SharedKernel;

public interface IDomainEvent
{
    public Guid EventId { get; }
}
