using Valt.Core.Common;

namespace Valt.Core.Modules.Budget.Accounts;

public record AccountGroupTotalCurrency
{
    public enum TotalCurrencyType
    {
        DefaultFiat,
        Bitcoin,
        SpecificFiat
    }

    public TotalCurrencyType Type { get; }
    public string? CurrencyCode { get; }

    private AccountGroupTotalCurrency(TotalCurrencyType type, string? currencyCode = null)
    {
        Type = type;
        CurrencyCode = currencyCode;
    }

    public static AccountGroupTotalCurrency DefaultFiat() => new(TotalCurrencyType.DefaultFiat);
    public static AccountGroupTotalCurrency Bitcoin() => new(TotalCurrencyType.Bitcoin);

    public static AccountGroupTotalCurrency Fiat(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code cannot be empty.", nameof(currencyCode));

        _ = FiatCurrency.GetFromCode(currencyCode);

        return new AccountGroupTotalCurrency(TotalCurrencyType.SpecificFiat, currencyCode);
    }

    public string ToStorageString()
    {
        return Type switch
        {
            TotalCurrencyType.DefaultFiat => "DEFAULT",
            TotalCurrencyType.Bitcoin => "BTC",
            TotalCurrencyType.SpecificFiat => CurrencyCode!,
            _ => "DEFAULT"
        };
    }

    public static AccountGroupTotalCurrency FromStorageString(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "DEFAULT")
            return DefaultFiat();

        if (value == "BTC")
            return Bitcoin();

        try
        {
            return Fiat(value);
        }
        catch
        {
            return DefaultFiat();
        }
    }

    public bool IsAvailable(IEnumerable<string>? availableCurrencyCodes = null)
    {
        if (Type == TotalCurrencyType.DefaultFiat)
            return true;

        if (Type == TotalCurrencyType.Bitcoin)
            return true;

        if (Type == TotalCurrencyType.SpecificFiat && CurrencyCode is not null)
        {
            if (!FiatCurrency.GetAll().Any(c => c.Code == CurrencyCode))
                return false;

            if (availableCurrencyCodes is not null)
            {
                return availableCurrencyCodes.Contains(CurrencyCode, StringComparer.OrdinalIgnoreCase);
            }

            return true;
        }

        return false;
    }

    public AccountGroupTotalCurrency FallbackToDefaultIfUnavailable(IEnumerable<string>? availableCurrencyCodes = null)
    {
        return IsAvailable(availableCurrencyCodes) ? this : DefaultFiat();
    }
}
