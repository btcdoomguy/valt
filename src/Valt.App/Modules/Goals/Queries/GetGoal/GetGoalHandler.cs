using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.App.Modules.Goals.Queries.GetGoal;

internal sealed class GetGoalHandler : IQueryHandler<GetGoalQuery, GoalDTO?>
{
    private readonly IGoalRepository _goalRepository;

    public GetGoalHandler(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository;
    }

    public async Task<GoalDTO?> HandleAsync(GetGoalQuery query, CancellationToken ct = default)
    {
        var goal = await _goalRepository.GetByIdAsync(new GoalId(query.GoalId));

        if (goal is null)
            return null;

        return MapToDto(goal);
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
