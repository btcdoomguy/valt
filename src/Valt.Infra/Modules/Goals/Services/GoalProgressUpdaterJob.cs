using Microsoft.Extensions.Logging;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

public record GoalProgressUpdated() : INotification;

internal class GoalProgressUpdaterJob : IBackgroundJob
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IGoalQueries _goalQueries;
    private readonly IGoalRepository _goalRepository;
    private readonly IGoalProgressCalculatorFactory _calculatorFactory;
    private readonly GoalProgressState _progressState;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly IClock _clock;
    private readonly ILogger<GoalProgressUpdaterJob> _logger;

    public string Name => "Goal Progress Updater";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.GoalProgressUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(1);

    public GoalProgressUpdaterJob(
        ILocalDatabase localDatabase,
        IGoalQueries goalQueries,
        IGoalRepository goalRepository,
        IGoalProgressCalculatorFactory calculatorFactory,
        GoalProgressState progressState,
        INotificationPublisher notificationPublisher,
        IClock clock,
        ILogger<GoalProgressUpdaterJob> logger)
    {
        _localDatabase = localDatabase;
        _goalQueries = goalQueries;
        _goalRepository = goalRepository;
        _calculatorFactory = calculatorFactory;
        _progressState = progressState;
        _notificationPublisher = notificationPublisher;
        _clock = clock;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[GoalProgressUpdaterJob] Started");
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        if (!_localDatabase.HasDatabaseOpen)
        {
            _logger.LogDebug("[GoalProgressUpdaterJob] Local database not open, skipping");
            return;
        }

        // Bootstrap phase: query database for stale goals on first run
        if (!_progressState.BootstrapCompleted)
        {
            _logger.LogInformation("[GoalProgressUpdaterJob] Bootstrap: checking database for stale goals");
            await RunDatabaseQueryCycleAsync();
            _progressState.MarkBootstrapCompleted();
            return;
        }

        // Normal operation: only run if flag is set
        if (!_progressState.HasStaleGoals)
        {
            return;
        }

        _logger.LogDebug("[GoalProgressUpdaterJob] Stale flag set, running update cycle");
        _progressState.ClearStaleFlag();
        await RunDatabaseQueryCycleAsync();
    }

    private async Task RunDatabaseQueryCycleAsync()
    {
        try
        {
            var staleGoals = await _goalQueries.GetStaleGoalsAsync();

            if (staleGoals.Count == 0)
            {
                _logger.LogDebug("[GoalProgressUpdaterJob] No stale goals to update");
                return;
            }

            _logger.LogInformation("[GoalProgressUpdaterJob] Found {Count} stale goals to update", staleGoals.Count);

            foreach (var staleGoal in staleGoals)
            {
                var typeName = (GoalTypeNames)staleGoal.TypeId;
                var input = new GoalProgressInput(
                    typeName,
                    staleGoal.GoalTypeJson,
                    staleGoal.From,
                    staleGoal.To);

                var calculator = _calculatorFactory.GetCalculator(typeName);
                var result = await calculator.CalculateProgressAsync(input);

                var goal = await _goalRepository.GetByIdAsync(new GoalId(staleGoal.Id));
                if (goal is null)
                {
                    _logger.LogWarning("[GoalProgressUpdaterJob] Goal {Id} not found, skipping", staleGoal.Id);
                    continue;
                }

                goal.UpdateProgress(result.Progress, result.UpdatedGoalType, _clock.GetCurrentDateTimeUtc());
                await _goalRepository.SaveAsync(goal);

                _logger.LogInformation("[GoalProgressUpdaterJob] Updated goal {Id} with progress {Progress}%",
                    staleGoal.Id, result.Progress);
            }

            _logger.LogInformation("[GoalProgressUpdaterJob] Successfully updated {Count} goals", staleGoals.Count);

            await _notificationPublisher.PublishAsync(new GoalProgressUpdated());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoalProgressUpdaterJob] Error during execution");
            throw;
        }
    }
}
