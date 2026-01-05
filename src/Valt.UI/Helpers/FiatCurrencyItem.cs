using System.Collections.Generic;
using System.Linq;
using Valt.Core.Common;

namespace Valt.UI.Helpers;

/// <summary>
/// Represents a fiat currency item for use in currency selector lists.
/// </summary>
public record FiatCurrencyItem(string Code, string DisplayName)
{
    /// <summary>
    /// Creates a FiatCurrencyItem from a currency code.
    /// </summary>
    public static FiatCurrencyItem FromCode(string code)
    {
        var currency = FiatCurrency.GetFromCode(code);
        return new FiatCurrencyItem(currency.Code, $"{currency.Code} ({currency.Symbol})");
    }

    /// <summary>
    /// Gets all available fiat currencies as FiatCurrencyItem list, excluding USD (which is mandatory).
    /// </summary>
    public static List<FiatCurrencyItem> GetAllExceptUsd()
    {
        return FiatCurrency.GetAll()
            .Where(c => c.Code != "USD")
            .Select(c => new FiatCurrencyItem(c.Code, $"{c.Code} ({c.Symbol})"))
            .OrderBy(c => c.Code)
            .ToList();
    }

    public override string ToString() => DisplayName;
}
