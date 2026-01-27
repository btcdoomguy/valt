using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Kernel.Notifications.Handlers;

/// <summary>
/// Handles FiatHistoryPriceUpdatedMessage by triggering dependent jobs.
/// This is sent when new historical fiat price data is stored.
/// </summary>
internal class FiatHistoryPriceUpdatedHandler : INotificationHandler<FiatHistoryPriceUpdatedMessage>
{
    private readonly BackgroundJobManager _manager;
    private readonly ILogger<FiatHistoryPriceUpdatedHandler> _logger;

    public FiatHistoryPriceUpdatedHandler(
        BackgroundJobManager manager,
        ILogger<FiatHistoryPriceUpdatedHandler> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public Task HandleAsync(FiatHistoryPriceUpdatedMessage @event)
    {
        _logger.LogInformation("[FiatHistoryPriceUpdatedHandler] Triggering AutoSatAmountUpdater and LivePricesUpdater jobs");
        _manager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        _manager.TriggerJobManually(BackgroundJobSystemNames.LivePricesUpdater);
        return Task.CompletedTask;
    }
}
