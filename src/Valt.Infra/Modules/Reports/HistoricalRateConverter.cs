using Valt.Core.Common;

namespace Valt.Infra.Modules.Reports;

/// <summary>
/// Converts amounts between currencies using per-date historical rates from an
/// <see cref="IReportDataProvider"/>. All conversions route through USD as the
/// intermediate currency (fiat rates are expressed as currency units per USD).
/// </summary>
public static class HistoricalRateConverter
{
    public const decimal SatoshisPerBitcoin = 100_000_000m;

    private const string BtcCode = "BTC";
    private const string SatsCode = "SATS";

    /// <summary>
    /// Converts <paramref name="amount"/> from <paramref name="sourceCurrency"/>
    /// (fiat code, BTC, or SATS) to the target fiat currency using the rates
    /// effective at <paramref name="date"/>.
    /// </summary>
    public static decimal ConvertToFiat(decimal amount, string? sourceCurrency, string targetCurrency, DateOnly date, IReportDataProvider provider)
    {
        if (string.IsNullOrEmpty(sourceCurrency) || sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var usdAmount = ConvertToUsd(amount, sourceCurrency, date, provider);

        if (targetCurrency.Equals(FiatCurrency.Usd.Code, StringComparison.OrdinalIgnoreCase))
            return usdAmount;

        var targetRate = provider.GetFiatRateAt(date, FiatCurrency.GetFromCode(targetCurrency));
        return usdAmount * targetRate;
    }

    /// <summary>
    /// Converts a fiat amount to satoshis using the rates effective at <paramref name="date"/>.
    /// </summary>
    public static long ConvertFiatToSats(decimal fiatAmount, string sourceCurrency, DateOnly date, IReportDataProvider provider)
    {
        var usdAmount = ConvertToUsd(fiatAmount, sourceCurrency, date, provider);
        var btcPriceUsd = provider.GetUsdBitcoinPriceAt(date);
        return (long)(usdAmount / btcPriceUsd * SatoshisPerBitcoin);
    }

    private static decimal ConvertToUsd(decimal amount, string sourceCurrency, DateOnly date, IReportDataProvider provider)
    {
        if (sourceCurrency.Equals(SatsCode, StringComparison.OrdinalIgnoreCase))
        {
            var btcPriceUsd = provider.GetUsdBitcoinPriceAt(date);
            return amount / SatoshisPerBitcoin * btcPriceUsd;
        }

        if (sourceCurrency.Equals(BtcCode, StringComparison.OrdinalIgnoreCase))
        {
            var btcPriceUsd = provider.GetUsdBitcoinPriceAt(date);
            return amount * btcPriceUsd;
        }

        var sourceRate = provider.GetFiatRateAt(date, FiatCurrency.GetFromCode(sourceCurrency));
        return amount / sourceRate;
    }
}
