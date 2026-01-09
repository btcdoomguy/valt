using System.Globalization;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

public class StaticCsvFiatHistoricalDataProvider : IFiatHistoricalDataProvider
{
    private const string BASE_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/";

    private readonly ILogger<StaticCsvFiatHistoricalDataProvider> _logger;

    private static readonly HashSet<FiatCurrency> SupportedCurrenciesSet = new(
    [
        FiatCurrency.Aud, FiatCurrency.Bgn, FiatCurrency.Brl, FiatCurrency.Cad,
        FiatCurrency.Chf, FiatCurrency.Cny, FiatCurrency.Czk, FiatCurrency.Dkk,
        FiatCurrency.Eur, FiatCurrency.Gbp, FiatCurrency.Hkd, FiatCurrency.Huf,
        FiatCurrency.Idr, FiatCurrency.Ils, FiatCurrency.Inr, FiatCurrency.Isk,
        FiatCurrency.Jpy, FiatCurrency.Krw, FiatCurrency.Mxn, FiatCurrency.Myr,
        FiatCurrency.Nok, FiatCurrency.Nzd, FiatCurrency.Php, FiatCurrency.Pln,
        FiatCurrency.Ron, FiatCurrency.Sek, FiatCurrency.Sgd, FiatCurrency.Thb,
        FiatCurrency.Try, FiatCurrency.Usd, FiatCurrency.Zar
    ]);

    public StaticCsvFiatHistoricalDataProvider(ILogger<StaticCsvFiatHistoricalDataProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "StaticCsv";
    public bool RequiresApiKey => false;
    public bool InitialDownloadSource => true;
    public IReadOnlySet<FiatCurrency> SupportedCurrencies => SupportedCurrenciesSet;

    public async Task<IEnumerable<IFiatHistoricalDataProvider.FiatPriceData>> GetPricesAsync(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<FiatCurrency> currencies)
    {
        var currencyList = currencies.Where(c => c != FiatCurrency.Usd && SupportedCurrenciesSet.Contains(c)).ToList();
        if (currencyList.Count == 0)
        {
            return [];
        }

        var result = new Dictionary<DateOnly, Dictionary<FiatCurrency, decimal>>();

        foreach (var currency in currencyList)
        {
            try
            {
                var prices = await DownloadCurrencyDataAsync(currency, startDate, endDate);
                foreach (var (date, price) in prices)
                {
                    if (!result.ContainsKey(date))
                    {
                        result[date] = new Dictionary<FiatCurrency, decimal>();
                    }
                    result[date][currency] = price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV data for currency {Currency}", currency.Code);
            }
        }

        return result.Select(kvp => new IFiatHistoricalDataProvider.FiatPriceData(
            kvp.Key,
            new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>(
                kvp.Value.Select(c => new IFiatHistoricalDataProvider.CurrencyAndPrice(c.Key, c.Value))
            )
        )).ToList();
    }

    private async Task<IEnumerable<(DateOnly Date, decimal Price)>> DownloadCurrencyDataAsync(
        FiatCurrency currency,
        DateOnly startDate,
        DateOnly endDate)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var url = $"{BASE_URL}{currency.Code}.csv";
        _logger.LogInformation("Downloading CSV data from {Url}", url);

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        var result = new List<(DateOnly Date, decimal Price)>();
        decimal lastValidPrice = 0;

        var line = await reader.ReadLineAsync();
        while (line is not null)
        {
            var split = line.Split(',');
            if (split.Length >= 2)
            {
                if (DateOnly.TryParse(split[0], out var date))
                {
                    if (date >= startDate && date <= endDate)
                    {
                        if (split[1] != "nan" && decimal.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        {
                            lastValidPrice = price;
                        }

                        if (lastValidPrice > 0)
                        {
                            result.Add((date, lastValidPrice));
                        }
                    }
                }
            }

            line = await reader.ReadLineAsync();
        }

        _logger.LogInformation("Downloaded {Count} records for currency {Currency}", result.Count, currency.Code);
        return result;
    }
}
