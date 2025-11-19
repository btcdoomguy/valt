using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal class CoinbaseProvider : IBitcoinPriceProvider
{
    private readonly IClock _clock;
    private readonly ILogger<CoinbaseProvider> _logger;
    public string Name => "coinbase";

    public CoinbaseProvider(IClock clock, ILogger<CoinbaseProvider> logger)
    {
        _clock = clock;
        _logger = logger;
    }

    public async Task<BtcPrice> GetAsync()
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
            var coinResponse = JsonSerializer.Deserialize<CryptoRatesResponse>(json, options);

            if (coinResponse?.Data == null)
            {
                throw new Exception("Failed to retrieve Bitcoin price data from Coinbase.");
            }

            var items = new List<BtcPrice.Item>();
            foreach (var fiatCurrency in FiatCurrency.GetAll())
            {
                var rateValue = coinResponse.Data.Rates.GetValueOrDefault(fiatCurrency.Code);

                if (rateValue == 0)
                    continue;
                
                items.Add(new BtcPrice.Item(fiatCurrency.Code, rateValue));
            }
            
            return new BtcPrice(_clock.GetCurrentDateTimeUtc(), true, items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Coinbase provider");
            throw;
        }
    }

    public record CryptoRatesResponse
    {
        [JsonPropertyName("data")]
        public CryptoRatesData Data { get; init; } = null!;
    }

    public record CryptoRatesData
    {
        [JsonPropertyName("currency")]
        public string BaseCurrency { get; init; } = string.Empty; 

        [JsonPropertyName("rates")]
        public Dictionary<string, string> RatesAsString { get; init; } = new();
    
        [JsonIgnore]
        public Dictionary<string, decimal> Rates => 
            _rates ??= RatesAsString.ToDictionary(
                kvp => kvp.Key, 
                kvp => decimal.Parse(kvp.Value, CultureInfo.InvariantCulture));
    
        private Dictionary<string, decimal>? _rates;
    }
}