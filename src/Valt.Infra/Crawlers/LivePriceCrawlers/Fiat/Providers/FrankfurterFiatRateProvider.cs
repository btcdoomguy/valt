using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Fiat.Providers;

public class FrankfurterFiatRateProvider : IFiatPriceProvider
{
    private readonly ILogger<FrankfurterFiatRateProvider> _logger;

    public FrankfurterFiatRateProvider(ILogger<FrankfurterFiatRateProvider> logger)
    {
        _logger = logger;
    }
    
    public string Name => "Frankfurter Fiat Rate";

    public async Task<IReadOnlyList<FiatUsdPrice>> GetAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        try
        {
            var response = await client.GetAsync($"https://api.frankfurter.dev/v1/latest?base=USD&symbols={string.Join(',', FiatCurrency.GetAll().Select(x => x.Code))}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(content);
                var rates = json.RootElement.GetProperty("rates");

                var ratesResponse = new List<FiatUsdPrice>();
                ratesResponse.Add(new FiatUsdPrice("USD", 1));
                foreach (var symbol in FiatCurrency.GetAll())
                {
                    if (rates.TryGetProperty(symbol.Code, out var rate))
                    {
                        ratesResponse.Add(new FiatUsdPrice(symbol.Code, rate.GetDecimal()));
                    }
                }
                return ratesResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Frankfurter Fiat Rate provider");
        }

        return [];
    }
}