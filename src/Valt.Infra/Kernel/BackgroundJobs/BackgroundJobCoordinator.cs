using CommunityToolkit.Mvvm.Messaging;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;

namespace Valt.Infra.Kernel.BackgroundJobs;

public class BackgroundJobCoordinator : IRecipient<BitcoinHistoryPriceUpdatedMessage>, IRecipient<FiatHistoryPriceUpdatedMessage>, IDisposable
{
    private readonly BackgroundJobManager _manager;

    public BackgroundJobCoordinator(BackgroundJobManager manager)
    {
        _manager = manager;
        
        WeakReferenceMessenger.Default.Register<BitcoinHistoryPriceUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Register<FiatHistoryPriceUpdatedMessage>(this);
    }

    public void Receive(BitcoinHistoryPriceUpdatedMessage message)
    {
        _manager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        _manager.TriggerJobManually(BackgroundJobSystemNames.LivePricesUpdater);
    }

    public void Receive(FiatHistoryPriceUpdatedMessage message)
    {
        _manager.TriggerJobManually(BackgroundJobSystemNames.AutoSatAmountUpdater);
        _manager.TriggerJobManually(BackgroundJobSystemNames.LivePricesUpdater);
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<BitcoinHistoryPriceUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<FiatHistoryPriceUpdatedMessage>(this);
    }
}