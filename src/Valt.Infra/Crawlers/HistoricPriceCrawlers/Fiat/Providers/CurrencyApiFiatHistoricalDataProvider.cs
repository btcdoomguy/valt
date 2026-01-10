using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

/// <summary>
/// A fallback historical data provider that fetches data date-by-date from the Currency API.
/// This provider is slower because it makes one HTTP request per date, but supports all currencies.
/// URL pattern: https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@{date}/v1/currencies/usd.json
/// </summary>
public class CurrencyApiFiatHistoricalDataProvider : IFiatHistoricalDataProvider
{
    private const string BASE_URL = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@";

    private readonly ILogger<CurrencyApiFiatHistoricalDataProvider> _logger;

    // Currency API supports all FiatCurrency codes
    private static readonly HashSet<FiatCurrency> CurrencyApiSupportedCurrencies = new(
    [
        FiatCurrency.Aud, FiatCurrency.Bgn, FiatCurrency.Brl, FiatCurrency.Cad,
        FiatCurrency.Chf, FiatCurrency.Cny, FiatCurrency.Czk, FiatCurrency.Dkk,
        FiatCurrency.Eur, FiatCurrency.Gbp, FiatCurrency.Hkd, FiatCurrency.Huf,
        FiatCurrency.Idr, FiatCurrency.Ils, FiatCurrency.Inr, FiatCurrency.Isk,
        FiatCurrency.Jpy, FiatCurrency.Krw, FiatCurrency.Mxn, FiatCurrency.Myr,
        FiatCurrency.Nok, FiatCurrency.Nzd, FiatCurrency.Php, FiatCurrency.Pln,
        FiatCurrency.Pyg, FiatCurrency.Ron, FiatCurrency.Sek, FiatCurrency.Sgd,
        FiatCurrency.Thb, FiatCurrency.Try, FiatCurrency.Usd, FiatCurrency.Uyu,
        FiatCurrency.Zar
    ]);

    public CurrencyApiFiatHistoricalDataProvider(ILogger<CurrencyApiFiatHistoricalDataProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "CurrencyApi";
    public bool RequiresApiKey => false;
    public bool InitialDownloadSource => false;
    public bool IsFallbackProvider => true;
    public IReadOnlySet<FiatCurrency> SupportedCurrencies => CurrencyApiSupportedCurrencies;

    public async Task<IEnumerable<IFiatHistoricalDataProvider.FiatPriceData>> GetPricesAsync(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<FiatCurrency> currencies)
    {
        var currencyList = currencies.Where(c => c != FiatCurrency.Usd).ToList();
        if (currencyList.Count == 0)
        {
            return [];
        }

        var result = new List<IFiatHistoricalDataProvider.FiatPriceData>();

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Fetch date-by-date, skipping weekends to save requests
        var currentDate = SkipWeekendsForward(startDate);
        while (currentDate <= endDate)
        {
            try
            {
                var priceData = await FetchPricesForDateAsync(client, currentDate, currencyList);
                if (priceData != null)
                {
                    result.Add(priceData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch prices for date {Date} from CurrencyApi", currentDate);
            }

            currentDate = SkipWeekendsForward(currentDate.AddDays(1));
        }

        return result;
    }

    private static DateOnly SkipWeekendsForward(DateOnly date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }

    private async Task<IFiatHistoricalDataProvider.FiatPriceData?> FetchPricesForDateAsync(
        HttpClient client,
        DateOnly date,
        List<FiatCurrency> currencies)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var url = $"{BASE_URL}{dateStr}/v1/currencies/usd.json";

        _logger.LogDebug("Fetching prices from CurrencyApi for date {Date}", dateStr);

        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CurrencyApi returned {StatusCode} for date {Date}", response.StatusCode, dateStr);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);

        if (!json.RootElement.TryGetProperty("usd", out var rates))
        {
            _logger.LogWarning("CurrencyApi response for {Date} missing 'usd' property", dateStr);
            return null;
        }

        var currencyPrices = new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>();

        foreach (var currency in currencies)
        {
            var currencyCode = currency.Code.ToLowerInvariant();
            if (rates.TryGetProperty(currencyCode, out var rate))
            {
                var price = rate.GetDecimal();
                currencyPrices.Add(new IFiatHistoricalDataProvider.CurrencyAndPrice(currency, price));
            }
            else
            {
                _logger.LogDebug("Currency {Currency} not found in CurrencyApi response for {Date}",
                    currency.Code, dateStr);
            }
        }

        if (currencyPrices.Count == 0)
        {
            return null;
        }

        return new IFiatHistoricalDataProvider.FiatPriceData(date, currencyPrices);
    }
}
