using Microsoft.Extensions.Logging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Kernel.Notifications.Handlers;

/// <summary>
/// Handles FiatHistoryRefreshRequestedMessage by triggering the FiatHistoryUpdater job.
/// This is sent when a transaction with a date earlier than the minimum fiat date is saved.
/// </summary>
internal class FiatHistoryRefreshRequestedHandler : INotificationHandler<FiatHistoryRefreshRequestedMessage>
{
    private readonly BackgroundJobManager _manager;
    private readonly ILogger<FiatHistoryRefreshRequestedHandler> _logger;

    public FiatHistoryRefreshRequestedHandler(
        BackgroundJobManager manager,
        ILogger<FiatHistoryRefreshRequestedHandler> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public Task HandleAsync(FiatHistoryRefreshRequestedMessage @event)
    {
        _logger.LogInformation("[FiatHistoryRefreshRequestedHandler] Triggering FiatHistoryUpdater job");
        _manager.TriggerJobManually(BackgroundJobSystemNames.FiatHistoryUpdater);
        return Task.CompletedTask;
    }
}
