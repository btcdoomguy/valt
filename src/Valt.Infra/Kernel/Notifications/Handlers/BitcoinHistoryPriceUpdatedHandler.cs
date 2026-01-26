using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Kernel.Notifications.Handlers;

/// <summary>
/// Handles BitcoinHistoryPriceUpdatedMessage by triggering dependent jobs.
/// This is sent when new historical Bitcoin price data is stored.
/// </summary>
internal class BitcoinHistoryPriceUpdatedHandler : INotificationHandler<BitcoinHistoryPriceUpdatedMessage>
{
    private readonly BackgroundJobManager _manager;
    private readonly ILogger<BitcoinHistoryPriceUpdatedHandler> _logger;

    public BitcoinHistoryPriceUpdatedHandler(
        BackgroundJobManager manager,
        ILogger<BitcoinHistoryPriceUpdatedHandler> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public Task HandleAsync(BitcoinHistoryPriceUpdatedMessage @event)
    {
        _logger.LogInformation("[BitcoinHistoryPriceUpdatedHandler] Triggering AutoSatAmountUpdater and LivePricesUpdater jobs");
        _manager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        _manager.TriggerJobManually(BackgroundJobSystemNames.LivePricesUpdater);
        return Task.CompletedTask;
    }
}
