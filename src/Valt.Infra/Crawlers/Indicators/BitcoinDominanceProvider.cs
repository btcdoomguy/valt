using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.Indicators;

using Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal class BitcoinDominanceProvider : IBitcoinDominanceProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BitcoinDominanceProvider> _logger;
    private readonly CoinGeckoRateLimiter _rateLimiter;

    public BitcoinDominanceProvider(IHttpClientFactory httpClientFactory, ILogger<BitcoinDominanceProvider> logger, CoinGeckoRateLimiter rateLimiter)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _rateLimiter = rateLimiter;
    }

    public async Task<BitcoinDominanceData> GetAsync()
    {
        await _rateLimiter.WaitAsync();

        using var client = _httpClientFactory.CreateClient(HttpClientNames.CoinGecko);
        try
        {
            var url = "https://api.coingecko.com/api/v3/global";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var data = root.GetProperty("data");
            var marketCapPercentage = data.GetProperty("market_cap_percentage");

            if (!marketCapPercentage.TryGetProperty("btc", out var btcDominance))
                throw new InvalidOperationException("Missing 'btc' in market_cap_percentage");

            var dominancePercent = Math.Round(btcDominance.GetDecimal(), 1);

            return new BitcoinDominanceData(dominancePercent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bitcoin Dominance from CoinGecko");
            throw;
        }
    }
}
