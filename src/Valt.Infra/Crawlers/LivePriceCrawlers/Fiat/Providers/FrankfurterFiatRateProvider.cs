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

    public FrankfurterFiatRateProvider(IClock clock, ILogger<FrankfurterFiatRateProvider> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public string Name => "Frankfurter";
    public IReadOnlySet<FiatCurrency> SupportedCurrencies => FrankfurterSupportedCurrencies;

    public async Task<FiatUsdPrice> GetAsync(IEnumerable<FiatCurrency> currencies)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var currencyList = currencies.Where(c => c != FiatCurrency.Usd).ToList();
            if (currencyList.Count == 0)
            {
                // No currencies to fetch, return just USD
                return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), true,
                    new[] { new FiatUsdPrice.Item(FiatCurrency.Usd, 1) });
            }

            var symbolsParam = string.Join(',', currencyList.Select(c => c.Code));
            var response = await client.GetAsync($"https://api.frankfurter.dev/v1/latest?base=USD&symbols={symbolsParam}");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException("Invalid API Response");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var rates = json.RootElement.GetProperty("rates");

            var ratesResponse = new List<FiatUsdPrice.Item>();
            ratesResponse.Add(new FiatUsdPrice.Item(FiatCurrency.Usd, 1));
            foreach (var currency in currencyList)
            {
                if (rates.TryGetProperty(currency.Code, out var rate))
                {
                    ratesResponse.Add(new FiatUsdPrice.Item(currency, rate.GetDecimal()));
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