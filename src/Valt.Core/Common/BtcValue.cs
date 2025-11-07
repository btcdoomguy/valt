using System.Globalization;
using Valt.Core.Common.Exceptions;

namespace Valt.Core.Common;

public record BtcValue
{
    public long Sats { get; init; }

    public decimal Btc => Convert.ToDecimal(Sats) / 100_000_000;

    private BtcValue(long sats)
    {
        if (sats < 0)
            throw new InvalidBtcValueException();
        Sats = sats;
    }

    public static BtcValue New(BtcValue value)
    {
        return new BtcValue(value.Sats);
    }

    public static BtcValue ParseSats(decimal value)
    {
        return new BtcValue(Convert.ToInt64(value));
    }

    public static BtcValue ParseBitcoin(decimal value)
    {
        return new BtcValue(Convert.ToInt64(value * 100_000_000));
    }

    public static BtcValue Empty => new((long)0);

    public override string ToString()
    {
        return Sats.ToString();
    }

    public string ToBitcoinString()
    {
        return (Sats / 100_000_000m).ToString(CultureInfo.InvariantCulture);
    }

    public static BtcValue operator +(BtcValue a, BtcValue b)
    {
        return new BtcValue(a.Sats + b.Sats);
    }

    public static BtcValue operator -(BtcValue a, BtcValue b)
    {
        return new BtcValue(a.Sats - b.Sats);
    }

    public static BtcValue operator *(BtcValue a, BtcValue b)
    {
        return new BtcValue(a.Sats * b.Sats);
    }

    public static BtcValue operator /(BtcValue a, BtcValue b)
    {
        return new BtcValue(a.Sats / b.Sats);
    }

    public static bool operator <=(BtcValue a, BtcValue b)
    {
        return a.Sats <= b.Sats;
    }

    public static bool operator >=(BtcValue a, BtcValue b)
    {
        return a.Sats >= b.Sats;
    }

    public static bool operator <(BtcValue a, BtcValue b)
    {
        return a.Sats < b.Sats;
    }

    public static bool operator >(BtcValue a, BtcValue b)
    {
        return a.Sats > b.Sats;
    }

    public static implicit operator long(BtcValue value) => value.Sats;

    public static implicit operator BtcValue(long value) => new BtcValue(value);
}