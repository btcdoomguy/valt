using Valt.Core.Modules.Budget.Transactions.Exceptions;

namespace Valt.Core.Modules.Budget.Transactions;

public class TransactionName : IEquatable<TransactionName>
{
    public string Value { get; }

    private TransactionName(string value)
    {
        Value = value;
    }

    public static TransactionName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyTransactionNameException();

        return new TransactionName(value);
    }

    public static implicit operator string(TransactionName name) => name.Value;

    public static implicit operator TransactionName(string name) => TransactionName.New(name);

    #region Equality members

    public static bool operator ==(TransactionName a, TransactionName b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(TransactionName a, TransactionName b)
    {
        return !(a == b);
    }

    public bool Equals(TransactionName? other)
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
        return Equals((TransactionName)obj);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    #endregion
}