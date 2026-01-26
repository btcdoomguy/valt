using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.App.Modules.Goals.Queries.GetGoals;

internal sealed class GetGoalsHandler : IQueryHandler<GetGoalsQuery, IReadOnlyList<GoalDTO>>
{
    private readonly IGoalRepository _goalRepository;

    public GetGoalsHandler(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<IReadOnlyList<GoalDTO>> HandleAsync(GetGoalsQuery query, CancellationToken ct = default)
    {
        var allGoals = await _goalRepository.GetAllAsync();

        IEnumerable<Goal> goals = allGoals;

        if (query.FilterDate.HasValue)
        {
            var filterDate = query.FilterDate.Value;
            goals = allGoals.Where(g =>
            {
                var range = g.GetPeriodRange();
                return filterDate >= range.Start && filterDate <= range.End;
            });
        }

        return goals
            .OrderBy(g => GetGoalSortOrder(g))
            .ThenBy(g => g.GoalType.TypeName)
            .ThenBy(g => g.RefDate)
            .Select(MapToDto)
            .ToList();
    }

    private static int GetGoalSortOrder(Goal goal)
    {
        return goal.State switch
        {
            GoalStates.Open => goal.Period == GoalPeriods.Monthly ? 0 : 1,
            GoalStates.Completed => 2,
            GoalStates.Failed => 3,
            _ => 4
        };
    }

    private static GoalDTO MapToDto(Goal goal)
    {
        return new GoalDTO
        {
            Id = goal.Id.Value,
            RefDate = goal.RefDate,
            Period = (int)goal.Period,
            Progress = goal.Progress,
            IsUpToDate = goal.IsUpToDate,
            LastUpdatedAt = goal.LastUpdatedAt,
            State = (int)goal.State,
            GoalType = MapGoalTypeToDto(goal.GoalType)
        };
    }

    private static GoalTypeOutputDTO MapGoalTypeToDto(IGoalType goalType)
    {
        return goalType switch
        {
            StackBitcoinGoalType stackBitcoin => new StackBitcoinGoalTypeOutputDTO
            {
                TargetSats = stackBitcoin.TargetSats,
                CalculatedSats = stackBitcoin.CalculatedSats
            },
            SpendingLimitGoalType spendingLimit => new SpendingLimitGoalTypeOutputDTO
            {
                TargetAmount = spendingLimit.TargetAmount,
                CalculatedSpending = spendingLimit.CalculatedSpending
            },
            DcaGoalType dca => new DcaGoalTypeOutputDTO
            {
                TargetPurchaseCount = dca.TargetPurchaseCount,
                CalculatedPurchaseCount = dca.CalculatedPurchaseCount
            },
            IncomeFiatGoalType incomeFiat => new IncomeFiatGoalTypeOutputDTO
            {
                TargetAmount = incomeFiat.TargetAmount,
                CalculatedIncome = incomeFiat.CalculatedIncome
            },
            IncomeBtcGoalType incomeBtc => new IncomeBtcGoalTypeOutputDTO
            {
                TargetSats = incomeBtc.TargetSats,
                CalculatedSats = incomeBtc.CalculatedSats
            },
            ReduceExpenseCategoryGoalType reduceExpense => new ReduceExpenseCategoryGoalTypeOutputDTO
            {
                TargetAmount = reduceExpense.TargetAmount,
                CategoryId = reduceExpense.CategoryId,
                CategoryName = reduceExpense.CategoryName,
                CalculatedSpending = reduceExpense.CalculatedSpending
            },
            BitcoinHodlGoalType bitcoinHodl => new BitcoinHodlGoalTypeOutputDTO
            {
                MaxSellableSats = bitcoinHodl.MaxSellableSats,
                CalculatedSoldSats = bitcoinHodl.CalculatedSoldSats
            },
            _ => throw new InvalidOperationException($"Unknown goal type: {goalType.GetType().Name}")
        };
    }
}
