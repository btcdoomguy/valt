using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.Indicators;

internal class BitcoinComIndicatorsProvider : IBitcoinComIndicatorsProvider
{
    private const string BaseUrl = "https://charts.bitcoin.com/api/v1/charts";
    private readonly ILogger<BitcoinComIndicatorsProvider> _logger;

    public BitcoinComIndicatorsProvider(ILogger<BitcoinComIndicatorsProvider> logger)
    {
        _logger = logger;
    }

    public async Task<MayerMultipleData> GetMayerMultipleAsync()
    {
        using var client = CreateClient();
        try
        {
            // Actual endpoint is /mayer, response has a "current" object
            var url = $"{BaseUrl}/mayer";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // { "current": { "price": 65840.8, "dma200": 97080.18, "mayerMultiple": 0.678 } }
            var current = root.GetProperty("current");
            var multiple = current.GetProperty("mayerMultiple").GetDecimal();
            var price = current.GetProperty("price").GetDecimal();
            var ma200 = current.GetProperty("dma200").GetDecimal();

            return new MayerMultipleData(Math.Round(multiple, 2), Math.Round(price, 2), Math.Round(ma200, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Mayer Multiple from Bitcoin.com");
            throw;
        }
    }

    public async Task<RainbowChartData> GetRainbowChartAsync()
    {
        using var client = CreateClient();
        try
        {
            var url = $"{BaseUrl}/rainbow";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // { "data": { "currentZone": { "name": "Still Cheap", ... }, "price": [...] } }
            var data = root.GetProperty("data");
            var currentZone = data.GetProperty("currentZone");
            var zoneName = currentZone.GetProperty("name").GetString() ?? "Unknown";

            // Get latest price from the price array
            var priceArray = data.GetProperty("price");
            var lastPrice = priceArray[priceArray.GetArrayLength() - 1];
            var price = lastPrice.GetProperty("price").GetDecimal();

            return new RainbowChartData(zoneName, Math.Round(price, 2));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Rainbow Chart from Bitcoin.com");
            throw;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        return client;
    }
}
