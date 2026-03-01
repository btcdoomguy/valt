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

    public async Task<PiCycleTopData> GetPiCycleTopAsync()
    {
        using var client = CreateClient();
        try
        {
            var url = $"{BaseUrl}/pi-cycle-top";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // { "data": { "price": [{ "timestamp", "price" }], "ma111": [{ "timestamp", "value" }], "ma350x2": [{ "timestamp", "value" }] } }
            var data = root.GetProperty("data");

            var priceArray = data.GetProperty("price");
            var lastPrice = priceArray[priceArray.GetArrayLength() - 1];
            var price = lastPrice.GetProperty("price").GetDecimal();

            var ma111Array = data.GetProperty("ma111");
            var lastMa111 = ma111Array[ma111Array.GetArrayLength() - 1];
            var ma111 = lastMa111.GetProperty("value").GetDecimal();

            var ma350x2Array = data.GetProperty("ma350x2");
            var lastMa350x2 = ma350x2Array[ma350x2Array.GetArrayLength() - 1];
            var ma350x2 = lastMa350x2.GetProperty("value").GetDecimal();

            // MAs are converging when the difference is less than 5%
            var convergenceThreshold = ma350x2 * 0.05m;
            var isConverging = Math.Abs(ma111 - ma350x2) < convergenceThreshold;

            return new PiCycleTopData(
                Math.Round(ma111, 2),
                Math.Round(ma350x2, 2),
                Math.Round(price, 2),
                isConverging);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pi Cycle Top from Bitcoin.com");
            throw;
        }
    }

    public async Task<StockToFlowData> GetStockToFlowAsync()
    {
        using var client = CreateClient();
        try
        {
            // Actual endpoint is /s2f
            var url = $"{BaseUrl}/s2f";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // { "current": { "predictedPrice": 673264.63, "actualPrice": 78653.66, "s2fRatio": 120.97 } }
            var current = root.GetProperty("current");
            var modelPrice = current.GetProperty("predictedPrice").GetDecimal();
            var actualPrice = current.GetProperty("actualPrice").GetDecimal();
            var ratio = modelPrice > 0 ? Math.Round(actualPrice / modelPrice, 2) : 0;

            return new StockToFlowData(
                Math.Round(modelPrice, 2),
                Math.Round(actualPrice, 2),
                ratio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Stock-to-Flow from Bitcoin.com");
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
