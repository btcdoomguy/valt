using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

public class FrankfurterFiatRateProvider : IFiatPriceProvider
{
    private readonly IClock _clock;
    private readonly ILogger<FrankfurterFiatRateProvider> _logger;

    public FrankfurterFiatRateProvider(IClock clock, ILogger<FrankfurterFiatRateProvider> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public string Name => "Frankfurter Fiat Rate";

    public async Task<FiatUsdPrice> GetAsync(IEnumerable<string> currencies)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var currencyList = currencies.Where(c => c != FiatCurrency.Usd.Code).ToList();
            if (currencyList.Count == 0)
            {
                // No currencies to fetch, return just USD
                return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), true,
                    new[] { new FiatUsdPrice.Item(FiatCurrency.Usd.Code, 1) });
            }

            var response = await client.GetAsync($"https://api.frankfurter.dev/v1/latest?base=USD&symbols={string.Join(',', currencyList)}");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException("Invalid API Response");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var rates = json.RootElement.GetProperty("rates");

            var ratesResponse = new List<FiatUsdPrice.Item>();
            ratesResponse.Add(new FiatUsdPrice.Item(FiatCurrency.Usd.Code, 1));
            foreach (var currencyCode in currencyList)
            {
                if (rates.TryGetProperty(currencyCode, out var rate))
                {
                    ratesResponse.Add(new FiatUsdPrice.Item(currencyCode, rate.GetDecimal()));
                }
            }

            return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), true, ratesResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Frankfurter Fiat Rate provider");
            throw;
        }
    }
}