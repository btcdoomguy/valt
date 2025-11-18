using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal class CoinbaseProvider : IBitcoinPriceProvider
{
    private readonly ILogger<CoinbaseProvider> _logger;
    public string Name => "coinbase";

    public CoinbaseProvider(ILogger<CoinbaseProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<BtcPriceMessage>> GetAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var coinResponse = JsonSerializer.Deserialize<CoinbaseResponse>(json, options);

            if (coinResponse?.data == null)
            {
                throw new Exception("Failed to retrieve Bitcoin price data from Coinbase.");
            }

            return new List<BtcPriceMessage>()
            {
                new(FiatCurrency.Usd.Code,
                    Convert.ToDecimal(coinResponse.data.rates!.USD, CultureInfo.InvariantCulture)),
                new(FiatCurrency.Eur.Code,
                    Convert.ToDecimal(coinResponse.data.rates.EUR, CultureInfo.InvariantCulture)),
                new(FiatCurrency.Brl.Code, Convert.ToDecimal(coinResponse.data.rates.BRL, CultureInfo.InvariantCulture))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Coinbase provider");
        }

        return [];
    }

    // ReSharper disable all InconsistentNaming
    internal class CoinbaseResponse
    {
        public Data? data { get; set; }

        internal class Data
        {
            public string? currency { get; set; }
            public Rates? rates { get; set; }
        }

        internal class Rates
        {
            public string? BRL { get; set; }
            public string? EUR { get; set; }
            public string? USD { get; set; }
        }
    }
}