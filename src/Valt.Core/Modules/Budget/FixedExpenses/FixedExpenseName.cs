using Valt.Core.Modules.Budget.FixedExpenses.Exceptions;

namespace Valt.Core.Modules.Budget.FixedExpenses;

public class FixedExpenseName : IEquatable<FixedExpenseName>
{
    public string Value { get; }

    private FixedExpenseName(string value)
    {
        Value = value;
    }

    public static FixedExpenseName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyFixedExpenseNameException();

        return new FixedExpenseName(value);
    }

    public static implicit operator string(FixedExpenseName name) => name.Value;

    public static implicit operator FixedExpenseName(string name) => FixedExpenseName.New(name);

    #region Equality members

    public static bool operator ==(FixedExpenseName a, FixedExpenseName b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(FixedExpenseName a, FixedExpenseName b)
    {
        return !(a == b);
    }

    public bool Equals(FixedExpenseName? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FixedExpenseName)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    #endregion
}