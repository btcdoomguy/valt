using System.Globalization;
using Microsoft.Extensions.Logging;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.DataSources.Bitcoin;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin;

public class BitcoinInitialSeedPriceProvider : IBitcoinInitialSeedPriceProvider
{
    private const string SEED_URL = "https://raw.githubusercontent.com/btcdoomguy/valt-data/refs/heads/master/initial-seed-price.csv";
    
    private readonly ILogger<BitcoinInitialSeedPriceProvider> _logger;

    public BitcoinInitialSeedPriceProvider(ILogger<BitcoinInitialSeedPriceProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task<IEnumerable<BitcoinPriceData>> GetPricesAsync()
    {
        using var client = new HttpClient();

        var result = new List<BitcoinPriceData>();
        try
        {
            var url = SEED_URL;

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var stream = await response.Content.ReadAsStreamAsync();
            
            using var reader = new StreamReader(stream);

            var nextLine = reader.ReadLine();
            decimal lastValidPrice = 0;
            while (nextLine is not null)
            {
                var split = nextLine.Split(',');

                if (split[1] != "nan")
                    lastValidPrice = decimal.Parse(split[1], CultureInfo.InvariantCulture);

                result.Add(new BitcoinPriceData(DateOnly.Parse(split[0]), lastValidPrice));

                nextLine = reader.ReadLine();
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