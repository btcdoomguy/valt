using Microsoft.Extensions.Logging;
using Valt.App.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Crawlers.HistoricPriceCrawlers.Messages;
using Valt.Infra.Kernel.Notifications;

namespace Valt.Infra.Modules.Goals.Handlers;

/// <summary>
/// Handles price update messages and marks relevant goals as stale.
/// This ensures that goals depending on currency conversion (like ReduceExpenseCategory)
/// are recalculated when historical prices are updated.
/// </summary>
internal class MarkGoalsStaleOnPriceUpdateHandler :
    INotificationHandler<FiatHistoryPriceUpdatedMessage>,
    INotificationHandler<BitcoinHistoryPriceUpdatedMessage>
{
    private readonly IGoalRepository _goalRepository;
    private readonly IGoalProgressState _progressState;
    private readonly ILogger<MarkGoalsStaleOnPriceUpdateHandler> _logger;

    public MarkGoalsStaleOnPriceUpdateHandler(
        IGoalRepository goalRepository,
        IGoalProgressState progressState,
        ILogger<MarkGoalsStaleOnPriceUpdateHandler> logger)
    {
        _goalRepository = goalRepository;
        _progressState = progressState;
        _logger = logger;
    }

    public Task HandleAsync(FiatHistoryPriceUpdatedMessage @event)
    {
        _logger.LogInformation("[MarkGoalsStaleOnPriceUpdate] Fiat history prices updated, marking price-dependent goals as stale");
        MarkPriceDependentGoalsStaleAndRefresh();
        return Task.CompletedTask;
    }

    public Task HandleAsync(BitcoinHistoryPriceUpdatedMessage @event)
    {
        _logger.LogInformation("[MarkGoalsStaleOnPriceUpdate] Bitcoin history prices updated, marking price-dependent goals as stale");
        MarkPriceDependentGoalsStaleAndRefresh();
        return Task.CompletedTask;
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
                _progressState.MarkAsStale();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MarkGoalsStaleOnPriceUpdate] Error marking goals as stale");
        }
    }
}
