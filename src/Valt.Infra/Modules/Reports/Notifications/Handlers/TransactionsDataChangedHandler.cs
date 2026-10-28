using Valt.App.Kernel.Notifications;
using Valt.Infra.Modules.Reports.Notifications;

namespace Valt.Infra.Modules.Reports.Notifications.Handlers;

/// <summary>
/// Invalidates the cached report data provider whenever transaction data changes,
/// so a flag toggle is reflected in Reports without an app restart.
/// </summary>
internal sealed class TransactionsDataChangedHandler : INotificationHandler<TransactionsDataChangedNotification>
{
    private readonly IReportDataProviderFactory _reportDataProviderFactory;

    public TransactionsDataChangedHandler(IReportDataProviderFactory reportDataProviderFactory)
    {
        _reportDataProviderFactory = reportDataProviderFactory;
    }

    public Task HandleAsync(TransactionsDataChangedNotification notification)
    {
        _reportDataProviderFactory.Invalidate();
        return Task.CompletedTask;
    }
}
