using Valt.Core.Common.Exceptions;
using Valt.Core.Modules.AvgPrice.Exceptions;

namespace Valt.Core.Modules.AvgPrice;

public record AvgPriceProfileName
{
    public string Value { get; }

    private AvgPriceProfileName(string value)
    {
        Value = value;
    }

    public static AvgPriceProfileName New(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new EmptyAvgPriceProfileException();

        if (value.Length > 30)
            throw new MaximumFieldLengthException(nameof(AvgPriceProfileName), 20);

        return new AvgPriceProfileName(value);
    }

    public static implicit operator string(AvgPriceProfileName name) => name.Value;

    public static implicit operator AvgPriceProfileName(string name) => New(name);
}