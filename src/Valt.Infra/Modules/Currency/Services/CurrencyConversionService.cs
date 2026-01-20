using System;
using System.Collections.Generic;
using Valt.Core.Common;

namespace Valt.Infra.Modules.Currency.Services;

/// <summary>
/// Converts amounts between different currencies (fiat and BTC) using provided rates.
/// Uses USD as an intermediate currency for fiat-to-fiat conversions.
/// </summary>
public class CurrencyConversionService : ICurrencyConversionService
{
    private const string BtcCode = "BTC";
    private const string SatsCode = "SATS";
    private const decimal SatsPerBtc = 100_000_000m;

    public decimal Convert(decimal amount, string fromCurrencyCode, string toCurrencyCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (amount == 0)
            return 0;

        if (string.Equals(fromCurrencyCode, toCurrencyCode, StringComparison.OrdinalIgnoreCase))
            return amount;

        var fromIsBtc = string.Equals(fromCurrencyCode, BtcCode, StringComparison.OrdinalIgnoreCase);
        var toIsBtc = string.Equals(toCurrencyCode, BtcCode, StringComparison.OrdinalIgnoreCase);
        var fromIsSats = string.Equals(fromCurrencyCode, SatsCode, StringComparison.OrdinalIgnoreCase);
        var toIsSats = string.Equals(toCurrencyCode, SatsCode, StringComparison.OrdinalIgnoreCase);

        // SATS -> SATS (handled by equality check above)
        // SATS -> BTC
        if (fromIsSats && toIsBtc)
            return amount / SatsPerBtc;

        // BTC -> SATS
        if (fromIsBtc && toIsSats)
            return amount * SatsPerBtc;

        // SATS -> Fiat: convert to BTC first, then to fiat
        if (fromIsSats)
        {
            var btcAmount = amount / SatsPerBtc;
            return ConvertBtcToFiat(btcAmount, toCurrencyCode, bitcoinPriceUsd, fiatRates);
        }

        // Fiat/BTC -> SATS: convert to BTC first, then to SATS
        if (toIsSats)
        {
            var btcAmount = fromIsBtc ? amount : ConvertFiatToBtc(amount, fromCurrencyCode, bitcoinPriceUsd, fiatRates);
            return btcAmount * SatsPerBtc;
        }

        // BTC -> Fiat
        if (fromIsBtc && !toIsBtc)
            return ConvertBtcToFiat(amount, toCurrencyCode, bitcoinPriceUsd, fiatRates);

        // Fiat -> BTC
        if (!fromIsBtc && toIsBtc)
            return ConvertFiatToBtc(amount, fromCurrencyCode, bitcoinPriceUsd, fiatRates);

        // Fiat -> Fiat
        return ConvertFiatToFiat(amount, fromCurrencyCode, toCurrencyCode, fiatRates);
    }

    public IReadOnlyDictionary<string, decimal> ConvertToAll(decimal amount, string fromCurrencyCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        var result = new Dictionary<string, decimal>();

        // Add BTC conversion
        result[BtcCode] = Convert(amount, fromCurrencyCode, BtcCode, bitcoinPriceUsd, fiatRates);

        // Add SATS conversion
        result[SatsCode] = Convert(amount, fromCurrencyCode, SatsCode, bitcoinPriceUsd, fiatRates);

        // Add all fiat conversions
        foreach (var currency in FiatCurrency.GetAll())
        {
            result[currency.Code] = Convert(amount, fromCurrencyCode, currency.Code, bitcoinPriceUsd, fiatRates);
        }

        return result;
    }

    /// <summary>
    /// BTC -> Fiat: (btcAmount * BitcoinPrice) * FiatRates[fiat]
    /// </summary>
    private decimal ConvertBtcToFiat(decimal btcAmount, string fiatCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (bitcoinPriceUsd is null or 0)
            return 0;

        var usdAmount = btcAmount * bitcoinPriceUsd.Value;

        // If target is USD, we're done
        if (string.Equals(fiatCode, FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
            return usdAmount;

        // Convert USD to target fiat
        var fiatRate = GetFiatRate(fiatCode, fiatRates);
        if (fiatRate == 0)
            return 0;

        return usdAmount * fiatRate;
    }

    /// <summary>
    /// Fiat -> BTC: (fiatAmount / FiatRates[fiat]) / BitcoinPrice
    /// </summary>
    private decimal ConvertFiatToBtc(decimal fiatAmount, string fiatCode,
        decimal? bitcoinPriceUsd, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (bitcoinPriceUsd is null or 0)
            return 0;

        // First convert fiat to USD
        decimal usdAmount;
        if (string.Equals(fiatCode, FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
        {
            usdAmount = fiatAmount;
        }
        else
        {
            var fiatRate = GetFiatRate(fiatCode, fiatRates);
            if (fiatRate == 0)
                return 0;

            usdAmount = fiatAmount / fiatRate;
        }

        // Then convert USD to BTC
        return usdAmount / bitcoinPriceUsd.Value;
    }

    /// <summary>
    /// Fiat A -> Fiat B: (amountA / FiatRates[A]) * FiatRates[B]
    /// </summary>
    private decimal ConvertFiatToFiat(decimal amount, string fromFiatCode, string toFiatCode,
        IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        // Convert to USD first
        decimal usdAmount;
        if (string.Equals(fromFiatCode, FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
        {
            usdAmount = amount;
        }
        else
        {
            var fromRate = GetFiatRate(fromFiatCode, fiatRates);
            if (fromRate == 0)
                return 0;

            usdAmount = amount / fromRate;
        }

        // Then convert to target fiat
        if (string.Equals(toFiatCode, FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
            return usdAmount;

        var toRate = GetFiatRate(toFiatCode, fiatRates);
        if (toRate == 0)
            return 0;

        return usdAmount * toRate;
    }

    private static decimal GetFiatRate(string fiatCode, IReadOnlyDictionary<string, decimal>? fiatRates)
    {
        if (fiatRates is null)
            return 0;

        // Use uppercase for lookup since rates are stored with uppercase currency codes
        return fiatRates.TryGetValue(fiatCode.ToUpperInvariant(), out var rate) ? rate : 0;
    }
}
