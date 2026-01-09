using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

public class FrankfurterFiatHistoricalDataProvider : IFiatHistoricalDataProvider
{
    private readonly ILogger<FrankfurterFiatHistoricalDataProvider> _logger;

    private static readonly HashSet<FiatCurrency> FrankfurterSupportedCurrencies = new(
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

    public FrankfurterFiatHistoricalDataProvider(ILogger<FrankfurterFiatHistoricalDataProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "Frankfurter";
    public bool RequiresApiKey => false;
    public IReadOnlySet<FiatCurrency> SupportedCurrencies => FrankfurterSupportedCurrencies;

    public async Task<IEnumerable<IFiatHistoricalDataProvider.FiatPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate, IEnumerable<FiatCurrency> currencies)
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var currencyList = currencies.Where(c => c != FiatCurrency.Usd).ToList();
            if (currencyList.Count == 0)
            {
                // No currencies to fetch
                return [];
            }

            var symbolsParam = string.Join(",", currencyList.Select(c => c.Code));
            var response = await client.GetAsync($"https://api.frankfurter.dev/v1/{startDate.ToString("yyyy-MM-dd")}..{endDate.ToString("yyyy-MM-dd")}?base=USD&symbols={symbolsParam}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var json = JsonDocument.Parse(content);
                var rates = json.RootElement.GetProperty("rates");

                var ratesDict = new Dictionary<DateOnly, Dictionary<string, double>>();
                foreach (var dateProperty in rates.EnumerateObject())
                {
                    var date = DateOnly.Parse(dateProperty.Name);
                    var currenciesFromResponse = dateProperty.Value;
                    var currencyDict = new Dictionary<string, double>();

                    foreach (var currencyProperty in currenciesFromResponse.EnumerateObject())
                    {
                        var currencyCode = currencyProperty.Name;
                        var rate = currencyProperty.Value.GetDouble();
                        currencyDict.Add(currencyCode, rate);
                    }

                    ratesDict.Add(date, currencyDict);
                }

                var result = new List<IFiatHistoricalDataProvider.FiatPriceData>();
                foreach (var (date, value) in ratesDict)
                {
                    var currencyPrices = new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>();

                    foreach (var (currencyCode, d) in value)
                    {
                        var currency = FiatCurrency.GetFromCode(currencyCode);
                        var price = Convert.ToDecimal(d);
                        currencyPrices.Add(new IFiatHistoricalDataProvider.CurrencyAndPrice(currency, price));
                    }

                    result.Add(new IFiatHistoricalDataProvider.FiatPriceData(date, currencyPrices));
                }

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Frankfurter Fiat Historical Data Provider");
        }

        return [];
    }
}