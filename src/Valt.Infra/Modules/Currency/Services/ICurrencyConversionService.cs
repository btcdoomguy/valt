using System.Collections.Generic;

namespace Valt.Infra.Modules.Currency.Services;

/// <summary>
/// Converts amounts between different currencies (fiat and BTC) using provided rates.
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrencyCode">The source currency code (e.g., "USD", "BRL", "BTC").</param>
    /// <param name="toCurrencyCode">The target currency code (e.g., "USD", "BRL", "BTC").</param>
    /// <param name="bitcoinPriceUsd">Current BTC price in USD.</param>
    /// <param name="fiatRates">Dictionary of fiat currency rates relative to USD.</param>
    /// <returns>The converted amount, or 0 if conversion is not possible.</returns>
    decimal Convert(decimal amount, string fromCurrencyCode, string toCurrencyCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates);

    /// <summary>
    /// Converts an amount to all available currencies.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="fromCurrencyCode">The source currency code (e.g., "USD", "BRL", "BTC").</param>
    /// <param name="bitcoinPriceUsd">Current BTC price in USD.</param>
    /// <param name="fiatRates">Dictionary of fiat currency rates relative to USD.</param>
    /// <returns>A dictionary mapping currency codes to converted amounts.</returns>
    IReadOnlyDictionary<string, decimal> ConvertToAll(decimal amount, string fromCurrencyCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates);
}
