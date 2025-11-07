using Valt.Core.Kernel.Abstractions.EventSystem;

namespace Valt.Core.Kernel;

public abstract class AggregateRoot<T> : Entity<T>, IEquatable<AggregateRoot<T>>
{
    public int Version { get; protected set; } = 1;
    public IReadOnlyCollection<IDomainEvent> Events => _events;

    private readonly HashSet<IDomainEvent> _events = [];
    private bool _versionIncremented;

    protected void AddEvent(IDomainEvent @event)
    {
        if (_events.Count == 0 && !_versionIncremented)
        {
            Version++;
            _versionIncremented = true;
        }

        _events.Add(@event);
    }

    public void ClearEvents() => _events.Clear();

    public bool Equals(AggregateRoot<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || EqualityComparer<T>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((AggregateRoot<T>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Id!);
    }
}