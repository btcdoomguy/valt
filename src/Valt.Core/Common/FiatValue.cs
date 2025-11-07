using System.Globalization;
using Valt.Core.Common.Exceptions;

namespace Valt.Core.Common;

public record FiatValue
{
    public decimal Value { get; init; }

    private FiatValue(decimal value)
    {
        if (value < 0)
            throw new InvalidFiatValueException();
        Value = Math.Round(value, 2);
    }

    public static FiatValue New(decimal value)
    {
        return new FiatValue(value);
    }

    public static FiatValue New(FiatValue value)
    {
        return new FiatValue(value.Value);
    }

    public static FiatValue Empty => new(0m);

    public override string ToString()
    {
        return Value.ToString("N2", CultureInfo.CurrentUICulture);
    }

    public string ToString(CultureInfo cultureInfo)
    {
        return Value.ToString("N2", cultureInfo);
    }

    public string ToCurrencyString(FiatCurrency currency)
    {
        if (currency.SymbolOnRight)
            return $"{ToString(CultureInfo.CurrentUICulture)} {currency.Symbol}";
        return $"{currency.Symbol} {ToString(CultureInfo.CurrentUICulture)}";
    }

    public static FiatValue operator +(FiatValue a, FiatValue b)
    {
        return new FiatValue(a.Value + b.Value);
    }

    public static FiatValue operator -(FiatValue a, FiatValue b)
    {
        return new FiatValue(a.Value - b.Value);
    }

    public static FiatValue operator *(FiatValue a, FiatValue b)
    {
        return new FiatValue(a.Value * b.Value);
    }

    public static FiatValue operator /(FiatValue a, FiatValue b)
    {
        return new FiatValue(a.Value / b.Value);
    }

    public static bool operator <=(FiatValue a, FiatValue b)
    {
        return a.Value <= b.Value;
    }

    public static bool operator >=(FiatValue a, FiatValue b)
    {
        return a.Value >= b.Value;
    }

    public static bool operator <(FiatValue a, FiatValue b)
    {
        return a.Value < b.Value;
    }

    public static bool operator >(FiatValue a, FiatValue b)
    {
        return a.Value > b.Value;
    }

    public static implicit operator decimal(FiatValue value) => value.Value;

    public static implicit operator FiatValue(decimal value) => new FiatValue(value);
}