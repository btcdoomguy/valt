using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

public class CurrencyApiFiatRateProvider : IFiatPriceProvider
{
    private readonly IClock _clock;
    private readonly ILogger<CurrencyApiFiatRateProvider> _logger;

    // Currency API supports all 34 FiatCurrency codes
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

    public CurrencyApiFiatRateProvider(IClock clock, ILogger<CurrencyApiFiatRateProvider> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public string Name => "CurrencyApi";
    public IReadOnlySet<FiatCurrency> SupportedCurrencies => CurrencyApiSupportedCurrencies;

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

            var response = await client.GetAsync("https://latest.currency-api.pages.dev/v1/currencies/usd.json");
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException("Invalid API Response");
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var rates = json.RootElement.GetProperty("usd");

            var ratesResponse = new List<FiatUsdPrice.Item>();
            ratesResponse.Add(new FiatUsdPrice.Item(FiatCurrency.Usd, 1));
            foreach (var currency in currencyList)
            {
                var currencyCode = currency.Code.ToLowerInvariant();
                if (rates.TryGetProperty(currencyCode, out var rate))
                {
                    ratesResponse.Add(new FiatUsdPrice.Item(currency, rate.GetDecimal()));
                }
            }

            return new FiatUsdPrice(_clock.GetCurrentDateTimeUtc(), true, ratesResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of CurrencyApi Fiat Rate provider");
            throw;
        }
    }
}
