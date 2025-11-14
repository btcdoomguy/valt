using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.HistoricPriceCrawlers.Bitcoin.Providers;

public class KrakenBitcoinHistoricalDataProvider : IBitcoinHistoricalDataProvider
{
    private readonly ILogger<KrakenBitcoinHistoricalDataProvider> _logger;
    public bool RequiresApiKey => false;

    private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public KrakenBitcoinHistoricalDataProvider(ILogger<KrakenBitcoinHistoricalDataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<BitcoinPriceData>> GetPricesAsync(DateOnly startDate,
        DateOnly endDate)
    {
        using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            //daily rate
            //https://docs.kraken.com/api/docs/rest-api/get-ohlc-data
            //kraken api brings only data from 2 years max

            var today = DateTime.UtcNow.Date;

            if (startDate.ToValtDateTime() < today.AddYears(-2))
                throw new Exception("Cannot retrieve historical data from Kraken. (Too old data)");

            var url =
                $"https://api.kraken.com/0/public/OHLC?pair=XBTUSD&interval=1440&since={ToUnixTimestamp(startDate.ToValtDateTime())}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var coinResponse = JsonSerializer.Deserialize<MarketDataDto>(json, options);

            if (coinResponse == null)
            {
                throw new Exception("Failed to retrieve Bitcoin price data from Kraken. (Empty response).");
            }

            if (coinResponse.Error.Length > 0)
            {
                throw new Exception("Failed to retrieve Bitcoin price data from Kraken (API ERROR).");
            }

            var candlesticks = coinResponse.Result.Candlesticks
                .Select(KrakenBitcoinHistoricalDataProvider.ParseCandlestick)
                .ToArray();

            var allData = candlesticks.Select(candlestick => new BitcoinPriceData(
                DateOnly.FromDateTime(FromUnixTimestamp(candlestick.Timestamp)),
                candlestick.ClosePrice));

            return allData.Where(x => x.Date <= endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during execution of Kraken Bitcoin Historical Data Provider");
        }
        
        return [];
    }

    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return (long)dateTime.ToUniversalTime().Subtract(UNIX_EPOCH).TotalSeconds;
    }

    public static DateTime FromUnixTimestamp(long timestamp)
    {
        return UNIX_EPOCH.AddSeconds(timestamp);
    }

    public static Candlestick ParseCandlestick(object[] elements)
    {
        if (elements.Length != 8)
        {
            throw new InvalidOperationException("Candlestick array must have exactly 8 elements.");
        }

        return new Candlestick(
            Timestamp: Convert.ToInt64(elements[0].ToString()),
            OpenPrice: Convert.ToDecimal(elements[1].ToString(), CultureInfo.InvariantCulture),
            HighPrice: Convert.ToDecimal(elements[2].ToString(), CultureInfo.InvariantCulture),
            LowPrice: Convert.ToDecimal(elements[3].ToString(), CultureInfo.InvariantCulture),
            ClosePrice: Convert.ToDecimal(elements[4].ToString(), CultureInfo.InvariantCulture),
            Vwap: Convert.ToDecimal(elements[5].ToString(), CultureInfo.InvariantCulture),
            Volume: Convert.ToDecimal(elements[6].ToString(), CultureInfo.InvariantCulture),
            Count: Convert.ToInt64(elements[7].ToString())
        );
    }

    public class MarketDataDto
    {
        [JsonPropertyName("error")] public string[] Error { get; set; } = Array.Empty<string>();

        [JsonPropertyName("result")] public ResultDataDto Result { get; set; } = null!;
    }

    public class ResultDataDto
    {
        [JsonPropertyName("XXBTZUSD")] public object[][] Candlesticks { get; set; } = Array.Empty<object[]>();

        [JsonPropertyName("last")] public long Last { get; set; }
    }

    public record Candlestick(
        long Timestamp,
        decimal OpenPrice,
        decimal HighPrice,
        decimal LowPrice,
        decimal ClosePrice,
        decimal Vwap,
        decimal Volume,
        long Count
    );
}