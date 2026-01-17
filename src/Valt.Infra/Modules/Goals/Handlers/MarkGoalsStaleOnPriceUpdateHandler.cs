using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel.BackgroundJobs;

namespace Valt.Infra.Modules.Goals.Handlers;

/// <summary>
/// Handles price update messages and marks relevant goals as stale.
/// This ensures that goals depending on currency conversion (like ReduceExpenseCategory)
/// are recalculated when historical prices are updated.
/// </summary>
public class MarkGoalsStaleOnPriceUpdateHandler :
    IRecipient<FiatHistoryPriceUpdatedMessage>,
    IRecipient<BitcoinHistoryPriceUpdatedMessage>,
    IDisposable
{
    private readonly IGoalRepository _goalRepository;
    private readonly BackgroundJobManager _backgroundJobManager;
    private readonly ILogger<MarkGoalsStaleOnPriceUpdateHandler> _logger;
    private bool _disposed;

    public MarkGoalsStaleOnPriceUpdateHandler(
        IGoalRepository goalRepository,
        BackgroundJobManager backgroundJobManager,
        ILogger<MarkGoalsStaleOnPriceUpdateHandler> logger)
    {
        _goalRepository = goalRepository;
        _backgroundJobManager = backgroundJobManager;
        _logger = logger;

        // Register to receive messages
        WeakReferenceMessenger.Default.Register<FiatHistoryPriceUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Register<BitcoinHistoryPriceUpdatedMessage>(this);
    }

    public void Receive(FiatHistoryPriceUpdatedMessage message)
    {
        _logger.LogInformation("[MarkGoalsStaleOnPriceUpdate] Fiat history prices updated, marking price-dependent goals as stale");
        MarkPriceDependentGoalsStaleAndRefresh();
    }

    public void Receive(BitcoinHistoryPriceUpdatedMessage message)
    {
        _logger.LogInformation("[MarkGoalsStaleOnPriceUpdate] Bitcoin history prices updated, marking price-dependent goals as stale");
        MarkPriceDependentGoalsStaleAndRefresh();
    }

    private void MarkPriceDependentGoalsStaleAndRefresh()
    {
        try
        {
            var goals = _goalRepository.GetAllAsync().GetAwaiter().GetResult();
            var markedCount = 0;

            foreach (var goal in goals)
            {
                // Only mark goals that depend on price data for calculation
                if (goal.IsUpToDate && goal.GoalType.RequiresPriceDataForCalculation)
                {
                    goal.MarkAsStale();
                    _goalRepository.SaveAsync(goal).GetAwaiter().GetResult();
                    markedCount++;
                }
            }

            if (markedCount > 0)
            {
                _logger.LogInformation("[MarkGoalsStaleOnPriceUpdate] Marked {Count} price-dependent goals as stale", markedCount);
                _backgroundJobManager.TriggerJobManually(BackgroundJobSystemNames.GoalProgressUpdater);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MarkGoalsStaleOnPriceUpdate] Error marking goals as stale");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        WeakReferenceMessenger.Default.Unregister<FiatHistoryPriceUpdatedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<BitcoinHistoryPriceUpdatedMessage>(this);

        _disposed = true;
    }
}
