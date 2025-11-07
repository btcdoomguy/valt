using System.Diagnostics.CodeAnalysis;

namespace Valt.Core.Kernel;

/// <summary>
/// Generic class for Ids. Can use multiple formats as base value.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class EntityId<T>(T value) : IEquatable<EntityId<T>>
{
    [NotNull] public T Value { get; } = value;

    public bool Equals(EntityId<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return ReferenceEquals(this, other) || EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((EntityId<T>)obj);
    }

    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);

    public static bool operator ==(EntityId<T>? a1, EntityId<T>? a2) => Equals(a1, a2);

    public static bool operator !=(EntityId<T>? a1, EntityId<T>? a2) => !Equals(a1, a2);

    public override string? ToString() => Value.ToString();
}