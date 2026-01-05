using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Fiat.Providers;

public class FrankfurterFiatHistoricalDataProvider : IFiatHistoricalDataProvider
{
    private readonly ILogger<FrankfurterFiatHistoricalDataProvider> _logger;
    public bool RequiresApiKey => false;

    public FrankfurterFiatHistoricalDataProvider(ILogger<FrankfurterFiatHistoricalDataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<IFiatHistoricalDataProvider.FiatPriceData>> GetPricesAsync(DateOnly startDate, DateOnly endDate, IEnumerable<string> currencies)
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        try
        {
            var currencyList = currencies.Where(c => c != FiatCurrency.Usd.Code).ToList();
            if (currencyList.Count == 0)
            {
                // No currencies to fetch
                return [];
            }

            var response = await client.GetAsync($"https://api.frankfurter.dev/v1/{startDate.ToString("yyyy-MM-dd")}..{endDate.ToString("yyyy-MM-dd")}?base=USD&symbols={string.Join(",", currencyList)}");
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
                        var currency = currencyProperty.Name;
                        var rate = currencyProperty.Value.GetDouble();
                        currencyDict.Add(currency, rate);
                    }

                    ratesDict.Add(date, currencyDict);
                }

                var result = new List<IFiatHistoricalDataProvider.FiatPriceData>();
                foreach (var (date, value) in ratesDict)
                {
                    var currencyPrices = new HashSet<IFiatHistoricalDataProvider.CurrencyAndPrice>();

                    foreach (var (currency, d) in value)
                    {
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