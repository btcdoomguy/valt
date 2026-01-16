using Microsoft.Extensions.Logging;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.DataAccess;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.Infra.Modules.Goals.Queries;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class GoalProgressUpdaterJob : IBackgroundJob
{
    private readonly ILocalDatabase _localDatabase;
    private readonly IGoalQueries _goalQueries;
    private readonly IGoalRepository _goalRepository;
    private readonly IGoalProgressCalculatorFactory _calculatorFactory;
    private readonly IClock _clock;
    private readonly ILogger<GoalProgressUpdaterJob> _logger;

    public string Name => "Goal Progress Updater";
    public BackgroundJobSystemNames SystemName => BackgroundJobSystemNames.GoalProgressUpdater;
    public BackgroundJobTypes JobType => BackgroundJobTypes.ValtDatabase;
    public TimeSpan Interval => TimeSpan.FromSeconds(30);

    public GoalProgressUpdaterJob(
        ILocalDatabase localDatabase,
        IGoalQueries goalQueries,
        IGoalRepository goalRepository,
        IGoalProgressCalculatorFactory calculatorFactory,
        IClock clock,
        ILogger<GoalProgressUpdaterJob> logger)
    {
        _localDatabase = localDatabase;
        _goalQueries = goalQueries;
        _goalRepository = goalRepository;
        _calculatorFactory = calculatorFactory;
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
        _logger.LogInformation("[GoalProgressUpdaterJob] Starting goal progress update cycle");

        if (!_localDatabase.HasDatabaseOpen)
        {
            _logger.LogInformation("[GoalProgressUpdaterJob] Local database not open, skipping");
            return;
        }

        try
        {
            var staleGoals = await _goalQueries.GetStaleGoalsAsync();

            if (staleGoals.Count == 0)
            {
                _logger.LogInformation("[GoalProgressUpdaterJob] No stale goals to update");
                return;
            }

            _logger.LogInformation("[GoalProgressUpdaterJob] Found {Count} stale goals to update", staleGoals.Count);

            foreach (var staleGoal in staleGoals)
            {
                var input = new GoalProgressInput(
                    staleGoal.TypeName,
                    staleGoal.GoalTypeJson,
                    staleGoal.From,
                    staleGoal.To);

                var calculator = _calculatorFactory.GetCalculator(staleGoal.TypeName);
                var progress = await calculator.CalculateProgressAsync(input);

                var goal = await _goalRepository.GetByIdAsync(new GoalId(staleGoal.Id));
                if (goal is null)
                {
                    _logger.LogWarning("[GoalProgressUpdaterJob] Goal {Id} not found, skipping", staleGoal.Id);
                    continue;
                }

                goal.UpdateProgress(progress, _clock.GetCurrentDateTimeUtc());
                await _goalRepository.SaveAsync(goal);

                _logger.LogInformation("[GoalProgressUpdaterJob] Updated goal {Id} with progress {Progress}%",
                    staleGoal.Id, progress);
            }

            _logger.LogInformation("[GoalProgressUpdaterJob] Successfully updated {Count} goals", staleGoals.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoalProgressUpdaterJob] Error during execution");
            throw;
        }
    }
}
