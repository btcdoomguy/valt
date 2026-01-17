namespace Valt.Core.Modules.Goals.GoalTypes;

public sealed class ReduceExpenseCategoryGoalType : IGoalType
{
    public GoalTypeNames TypeName => GoalTypeNames.ReduceExpenseCategory;

    public bool RequiresPriceDataForCalculation => true;

    public ProgressionMode ProgressionMode => ProgressionMode.DecreasingSuccess;

    public decimal TargetAmount { get; }

    public string CategoryId { get; }

    public string CategoryName { get; }

    public decimal CalculatedSpending { get; }

    public ReduceExpenseCategoryGoalType(decimal targetAmount, string categoryId, string categoryName, decimal calculatedSpending = 0)
    {
        TargetAmount = targetAmount;
        CategoryId = categoryId;
        CategoryName = categoryName;
        CalculatedSpending = calculatedSpending;
    }

    public ReduceExpenseCategoryGoalType WithCalculatedSpending(decimal calculatedSpending)
    {
        return new ReduceExpenseCategoryGoalType(TargetAmount, CategoryId, CategoryName, calculatedSpending);
    }
}
