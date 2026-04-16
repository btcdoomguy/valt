using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.LivePriceCrawlers.Messages;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

internal class ThrottledBitcoinPriceProvider : IBitcoinPriceProvider
{
    private readonly IBitcoinPriceProvider _inner;
    private readonly ILogger<ThrottledBitcoinPriceProvider> _logger;
    private readonly Lock _lock = new();
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);

    private BtcPrice? _cachedResult;
    private DateTime _lastCallUtc = DateTime.MinValue;

    public string Name => _inner.Name;

    public ThrottledBitcoinPriceProvider(IBitcoinPriceProvider inner, ILogger<ThrottledBitcoinPriceProvider> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<BtcPrice> GetAsync()
    {
        lock (_lock)
        {
            if (_cachedResult is not null && DateTime.UtcNow - _lastCallUtc < _cacheDuration)
            {
                _logger.LogInformation("[ThrottledBitcoinPriceProvider] Returning cached result (age: {Age:N1}s)",
                    (DateTime.UtcNow - _lastCallUtc).TotalSeconds);
                return _cachedResult;
            }
        }

        var result = await _inner.GetAsync();

        lock (_lock)
        {
            _cachedResult = result;
            _lastCallUtc = DateTime.UtcNow;
        }

        return result;
    }
}
