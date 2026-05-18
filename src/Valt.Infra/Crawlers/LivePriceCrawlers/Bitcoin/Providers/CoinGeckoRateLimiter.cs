using Microsoft.Extensions.Logging;

namespace Valt.Infra.Crawlers.LivePriceCrawlers.Bitcoin.Providers;

public sealed class CoinGeckoRateLimiter
{
    private readonly Lock _lock = new();
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(5);
    private readonly ILogger<CoinGeckoRateLimiter> _logger;
    private DateTime _lastCallUtc = DateTime.MinValue;

    public CoinGeckoRateLimiter(ILogger<CoinGeckoRateLimiter> logger)
    {
        _logger = logger;
    }

    public async Task WaitAsync(CancellationToken ct = default)
    {
        TimeSpan delay;
        lock (_lock)
        {
            var elapsed = DateTime.UtcNow - _lastCallUtc;
            delay = elapsed < _minInterval ? _minInterval - elapsed : TimeSpan.Zero;
            if (delay > TimeSpan.Zero)
                _logger.LogInformation("[CoinGeckoRateLimiter] Delaying CoinGecko call by {DelayMs}ms to respect rate limit", delay.TotalMilliseconds);
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, ct);

        lock (_lock)
        {
            _lastCallUtc = DateTime.UtcNow;
        }
    }
}
