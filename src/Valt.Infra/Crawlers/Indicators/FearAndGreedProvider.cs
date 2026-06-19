using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.Indicators;

internal class FearAndGreedProvider : IFearAndGreedProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FearAndGreedProvider> _logger;

    public FearAndGreedProvider(IHttpClientFactory httpClientFactory, ILogger<FearAndGreedProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FearAndGreedData> GetAsync()
    {
        using var client = _httpClientFactory.CreateClient(HttpClientNames.Indicator);
        try
        {
            var url = "https://api.alternative.me/fng/?limit=1&format=json";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var dataArray) ||
                dataArray.ValueKind != JsonValueKind.Array ||
                dataArray.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Invalid Fear & Greed API response: missing data array");
            }

            var item = dataArray[0];
            var valueStr = item.GetProperty("value").GetString()
                           ?? throw new InvalidOperationException("Missing 'value' in Fear & Greed response");
            var classification = item.GetProperty("value_classification").GetString()
                                 ?? "Unknown";

            if (!int.TryParse(valueStr, out var value))
                throw new InvalidOperationException($"Cannot parse Fear & Greed value: {valueStr}");

            return new FearAndGreedData(value, classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Fear & Greed Index from Alternative.me");
            throw;
        }
    }
}
