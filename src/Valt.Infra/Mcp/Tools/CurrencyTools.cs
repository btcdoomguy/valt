using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.Core.Common;
using Valt.Infra.Crawlers.HistoricPriceCrawlers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;
using Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;
using Valt.Infra.Modules.Configuration;
using Valt.Infra.Modules.Currency.Services;
using Valt.Infra.Settings;

namespace Valt.Infra.Mcp.Tools;

/// <summary>
/// MCP tools for currency-related operations.
/// </summary>
[McpServerToolType]
public class CurrencyTools
{
    /// <summary>
    /// Gets the list of available and in-use fiat currencies.
    /// </summary>
    [McpServerTool, Description("Get available fiat currencies and currencies currently in use by accounts, fixed expenses, and avg price profiles")]
    public static AvailableCurrenciesResultDto GetAvailableCurrencies(
        IConfigurationManager configurationManager)
    {
        var available = configurationManager.GetAvailableFiatCurrencies();
        var inUse = configurationManager.GetCurrenciesInUse();

        return new AvailableCurrenciesResultDto
        {
            AvailableCurrencies = available,
            CurrenciesInUse = inUse
        };
    }

    /// <summary>
    /// Gets the configured main fiat currency.
    /// </summary>
    [McpServerTool, Description("Get the main fiat currency configured for the application")]
    public static MainCurrencyResultDto GetMainCurrency(
        CurrencySettings currencySettings)
    {
        var mainCurrencyCode = currencySettings.MainFiatCurrency;
        var currency = FiatCurrency.GetFromCode(mainCurrencyCode);

        return new MainCurrencyResultDto
        {
            MainCurrency = currency.Code,
            Details = new CurrencyDetailsDto
            {
                Code = currency.Code,
                Decimals = currency.Decimals,
                Symbol = currency.Symbol,
                SymbolOnRight = currency.SymbolOnRight
            }
        };
    }

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    [McpServerTool, Description("Convert an amount between currencies (USD, BRL, BTC, SATS, etc.). Uses live rates when available, falls back to historical rates.")]
    public static async Task<ConversionResultDto> ConvertCurrency(
        ICurrencyConversionService conversionService,
        IBitcoinPriceProvider bitcoinPriceProvider,
        IFiatPriceProviderSelector fiatPriceProviderSelector,
        ILocalHistoricalPriceProvider historicalPriceProvider,
        [Description("Amount to convert")] decimal amount,
        [Description("Source currency code (e.g., 'USD', 'BRL', 'BTC', 'SATS')")] string fromCurrency,
        [Description("Target currency code (e.g., 'USD', 'BRL', 'BTC', 'SATS')")] string toCurrency)
    {
        decimal? bitcoinPriceUsd = null;
        IReadOnlyDictionary<string, decimal>? fiatRates = null;
        var usedLiveRates = false;

        // Normalize currency codes
        var from = fromCurrency.ToUpperInvariant();
        var to = toCurrency.ToUpperInvariant();

        try
        {
            // Try to fetch live BTC price
            var btcPrice = await bitcoinPriceProvider.GetAsync();
            var usdItem = btcPrice.Items.FirstOrDefault(i => i.CurrencyCode == "USD");
            if (usdItem != null)
            {
                bitcoinPriceUsd = usdItem.Price;
                usedLiveRates = true;
            }
        }
        catch
        {
            // Fall back to historical price
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            bitcoinPriceUsd = await historicalPriceProvider.GetUsdBitcoinRateAtAsync(yesterday);
        }

        try
        {
            // Get all configured fiat currencies for rate fetching
            var allFiatCurrencies = FiatCurrency.GetAll().ToList();
            var fiatPrices = await fiatPriceProviderSelector.GetAsync(allFiatCurrencies);

            var ratesDict = new Dictionary<string, decimal>();
            foreach (var item in fiatPrices.Items)
            {
                ratesDict[item.Currency.Code] = item.Price;
            }
            fiatRates = ratesDict;
        }
        catch
        {
            // Fall back to historical fiat rates
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var historicalRates = await historicalPriceProvider.GetAllFiatRatesAtAsync(yesterday);

            var ratesDict = new Dictionary<string, decimal>();
            foreach (var rate in historicalRates)
            {
                ratesDict[rate.Currency.Code] = rate.Rate;
            }
            fiatRates = ratesDict;
            usedLiveRates = false;
        }

        var convertedAmount = conversionService.Convert(amount, from, to, bitcoinPriceUsd, fiatRates);

        return new ConversionResultDto
        {
            OriginalAmount = amount,
            FromCurrency = from,
            ToCurrency = to,
            ConvertedAmount = convertedAmount,
            BitcoinPriceUsd = bitcoinPriceUsd,
            UsedLiveRates = usedLiveRates
        };
    }

    /// <summary>
    /// Gets the historical price for BTC or a fiat currency at a specific date.
    /// </summary>
    [McpServerTool, Description("Get historical price for BTC (in USD) or fiat currencies (relative to USD) at a specific date")]
    public static async Task<HistoricalPriceResultDto> GetHistoricalPrice(
        ILocalHistoricalPriceProvider historicalPriceProvider,
        [Description("Date in yyyy-MM-dd format")] string date,
        [Description("Currency code: 'BTC' for Bitcoin price in USD, or fiat code (EUR, BRL, etc.) for rate relative to USD")] string currencyCode)
    {
        var parsedDate = DateOnly.Parse(date);
        var code = currencyCode.ToUpperInvariant();

        if (code == "BTC")
        {
            var price = await historicalPriceProvider.GetUsdBitcoinRateAtAsync(parsedDate);
            return new HistoricalPriceResultDto
            {
                Date = parsedDate.ToString("yyyy-MM-dd"),
                Currency = "BTC",
                Price = price,
                Found = price.HasValue,
                Description = price.HasValue
                    ? $"Bitcoin price was ${price.Value:N2} USD on {parsedDate:yyyy-MM-dd}"
                    : $"No Bitcoin price data found for {parsedDate:yyyy-MM-dd}"
            };
        }

        // Fiat currency rate
        var currency = FiatCurrency.GetFromCode(code);
        var rate = await historicalPriceProvider.GetFiatRateAtAsync(parsedDate, currency);

        return new HistoricalPriceResultDto
        {
            Date = parsedDate.ToString("yyyy-MM-dd"),
            Currency = currency.Code,
            Price = rate,
            Found = rate.HasValue,
            Description = rate.HasValue
                ? $"1 USD = {rate.Value:N4} {currency.Code} on {parsedDate:yyyy-MM-dd}"
                : $"No {currency.Code} rate data found for {parsedDate:yyyy-MM-dd}"
        };
    }
}

#region DTOs

public class AvailableCurrenciesResultDto
{
    public required IReadOnlyList<string> AvailableCurrencies { get; init; }
    public required IReadOnlyList<string> CurrenciesInUse { get; init; }
}

public class MainCurrencyResultDto
{
    public required string MainCurrency { get; init; }
    public required CurrencyDetailsDto Details { get; init; }
}

public class CurrencyDetailsDto
{
    public required string Code { get; init; }
    public required int Decimals { get; init; }
    public required string Symbol { get; init; }
    public required bool SymbolOnRight { get; init; }
}

public class ConversionResultDto
{
    public required decimal OriginalAmount { get; init; }
    public required string FromCurrency { get; init; }
    public required string ToCurrency { get; init; }
    public required decimal ConvertedAmount { get; init; }
    public decimal? BitcoinPriceUsd { get; init; }
    public required bool UsedLiveRates { get; init; }
}

public class HistoricalPriceResultDto
{
    public required string Date { get; init; }
    public required string Currency { get; init; }
    public decimal? Price { get; init; }
    public required bool Found { get; init; }
    public required string Description { get; init; }
}

#endregion
