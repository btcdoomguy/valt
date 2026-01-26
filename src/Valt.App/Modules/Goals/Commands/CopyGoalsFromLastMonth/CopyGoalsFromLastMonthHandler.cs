using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Infra.Modules.Goals.Services;

namespace Valt.App.Modules.Goals.Commands.CopyGoalsFromLastMonth;

internal sealed class CopyGoalsFromLastMonthHandler : ICommandHandler<CopyGoalsFromLastMonthCommand, CopyGoalsFromLastMonthResult>
{
    private readonly IGoalRepository _goalRepository;
    private readonly GoalProgressState _goalProgressState;

    public CopyGoalsFromLastMonthHandler(
        IGoalRepository goalRepository,
        GoalProgressState goalProgressState)
    {
        _goalRepository = goalRepository;
        _goalProgressState = goalProgressState;
    }

    public async Task<Result<CopyGoalsFromLastMonthResult>> HandleAsync(
        CopyGoalsFromLastMonthCommand command,
        CancellationToken ct = default)
    {
        var currentMonthStart = new DateOnly(command.CurrentDate.Year, command.CurrentDate.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        var allGoals = await _goalRepository.GetAllAsync();

        var previousMonthGoals = allGoals
            .Where(g => g.Period == GoalPeriods.Monthly &&
                       g.RefDate.Year == previousMonthStart.Year &&
                       g.RefDate.Month == previousMonthStart.Month)
            .ToList();

        var currentMonthGoals = allGoals
            .Where(g => g.Period == GoalPeriods.Monthly &&
                       g.RefDate.Year == currentMonthStart.Year &&
                       g.RefDate.Month == currentMonthStart.Month)
            .ToList();

        var copiedCount = 0;

        foreach (var previousGoal in previousMonthGoals)
        {
            // Check if a goal with the same target already exists in the current month
            var isDuplicate = currentMonthGoals.Any(g =>
                g.GoalType.HasSameTargetAs(previousGoal.GoalType));

            if (isDuplicate)
                continue;

            // Create a new goal with reset progress
            var newGoalType = previousGoal.GoalType.WithResetProgress();
            var newGoal = Goal.New(currentMonthStart, GoalPeriods.Monthly, newGoalType);
            await _goalRepository.SaveAsync(newGoal);
            copiedCount++;
        }

        if (copiedCount > 0)
        {
            _goalProgressState.MarkAsStale();
        }

        return Result<CopyGoalsFromLastMonthResult>.Success(
            new CopyGoalsFromLastMonthResult { CopiedCount = copiedCount });
    }
}
