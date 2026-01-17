using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;
using Valt.Infra.Modules.Goals.Queries.DTOs;

namespace Valt.Infra.Modules.Goals.Services;

internal class ReduceExpenseCategoryProgressCalculator : IGoalProgressCalculator
{
    private readonly IGoalTransactionReader _transactionReader;

    public GoalTypeNames SupportedType => GoalTypeNames.ReduceExpenseCategory;

    public ReduceExpenseCategoryProgressCalculator(IGoalTransactionReader transactionReader)
    {
        _transactionReader = transactionReader;
    }

    public Task<GoalProgressResult> CalculateProgressAsync(GoalProgressInput input)
    {
        var dto = JsonSerializer.Deserialize<ReduceExpenseCategoryGoalTypeDto>(input.GoalTypeJson)
                  ?? throw new InvalidOperationException("Failed to deserialize ReduceExpenseCategoryGoalType");
        var config = new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, dto.CategoryName, dto.CalculatedSpending);

        var targetCategoryId = new ObjectId(config.CategoryId);

        // Calculate total spending for the specific category in main fiat currency
        var totalSpending = _transactionReader.CalculateTotalExpenses(input.From, input.To, targetCategoryId);

        // Calculate percentage (0-100%): 0% = nothing spent, 100% = at/over limit (failed)
        var progress = config.TargetAmount > 0
            ? Math.Min(100m, (totalSpending * 100m) / config.TargetAmount)
            : (totalSpending == 0 ? 0m : 100m);

        // Create updated goal type with calculated values
        var updatedGoalType = config.WithCalculatedSpending(Math.Round(totalSpending, 2));

        return Task.FromResult(new GoalProgressResult(progress, updatedGoalType));
    }
}
