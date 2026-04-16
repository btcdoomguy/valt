using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal class CoinGeckoProvider : IBitcoinPriceProvider
{
    private readonly IClock _clock;
    private readonly ILogger<CoinGeckoProvider> _logger;
    public string Name => "coingecko";

    public CoinGeckoProvider(IClock clock, ILogger<CoinGeckoProvider> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public async Task<BtcPrice> GetAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Valt/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        try
        {
            var currencies = FiatCurrency.GetAll();
            var currencyCodes = string.Join(",", currencies.Select(c => c.Code.ToLowerInvariant()));
            var url = $"https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies={currencyCodes}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("CoinGecko raw response: {Json}", json);

            using var doc = JsonDocument.Parse(json);
            var bitcoin = doc.RootElement.GetProperty("bitcoin");

            var items = new List<BtcPrice.Item>();
            foreach (var fiatCurrency in currencies)
            {
                var code = fiatCurrency.Code.ToLowerInvariant();
                if (bitcoin.TryGetProperty(code, out var priceElement))
                {
                    var price = priceElement.GetDecimal();
                    if (price > 0)
                        items.Add(new BtcPrice.Item(fiatCurrency.Code, price));
                }
            }

            return new BtcPrice(_clock.GetCurrentDateTimeUtc(), true, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of CoinGecko provider");
            throw;
        }
    }
}
