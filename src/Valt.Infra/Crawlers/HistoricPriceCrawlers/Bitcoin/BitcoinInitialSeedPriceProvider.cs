using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

internal class BitcoinInitialSeedPriceProvider : IBitcoinInitialSeedPriceProvider
{
    private const string SEED_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/initial-seed-price.csv";
    
    private readonly ILogger<BitcoinInitialSeedPriceProvider> _logger;

    public BitcoinInitialSeedPriceProvider(ILogger<BitcoinInitialSeedPriceProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<IEnumerable<BitcoinPriceData>> GetPricesAsync()
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };

        var result = new List<BitcoinPriceData>();
        try
        {
            var url = SEED_URL;

            // Use ReadAsStringAsync to ensure full content is downloaded within timeout
            var content = await client.GetStringAsync(url).ConfigureAwait(false);

            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            decimal lastValidPrice = 0;

            foreach (var line in lines)
            {
                var split = line.Split(',');
                if (split.Length < 2) continue;

                if (split[1] != "nan")
                    lastValidPrice = decimal.Parse(split[1], CultureInfo.InvariantCulture);

                result.Add(new BitcoinPriceData(DateOnly.Parse(split[0]), lastValidPrice));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Bitcoin Initial Seed Price provider");
        }

        return [];
    }
}