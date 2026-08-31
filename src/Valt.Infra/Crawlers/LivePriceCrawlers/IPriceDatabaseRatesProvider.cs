using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers;

/// <summary>
/// Provides the latest available rates stored in the price database.
/// </summary>
public interface IPriceDatabaseRatesProvider
{
    /// <summary>
    /// Loads the latest BTC and fiat rates from the price database for all configured currencies.
    /// Returns null when the price database is not open, the local database is not open,
    /// or no stored rates are available.
    /// The returned message is always marked as not up-to-date because it reflects cached data.
    /// </summary>
    Task<LivePriceUpdateMessage?> GetLatestRatesAsync(CancellationToken cancellationToken = default);
}
