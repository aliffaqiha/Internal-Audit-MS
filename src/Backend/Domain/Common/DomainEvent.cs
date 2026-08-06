namespace IAMS.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent() => OccurredOn = DateTime.UtcNow;
    public DateTime OccurredOn { get; }
}