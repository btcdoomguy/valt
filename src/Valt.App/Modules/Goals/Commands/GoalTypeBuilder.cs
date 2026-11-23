using Valt.App.Kernel;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.App.Modules.Goals.Commands;

internal static class GoalTypeBuilder
{
    public static async Task<Result<IGoalType>> BuildGoalTypeAsync(
        ICategoryRepository categoryRepository,
        GoalTypeInputDTO dto)
    {
        return dto switch
        {
            StackBitcoinGoalTypeDTO stackBitcoin =>
                Result<IGoalType>.Success(new StackBitcoinGoalType(stackBitcoin.TargetSats)),

            SpendingLimitGoalTypeDTO spendingLimit =>
                Result<IGoalType>.Success(new SpendingLimitGoalType(spendingLimit.TargetAmount)),

            DcaGoalTypeDTO dca =>
                Result<IGoalType>.Success(new DcaGoalType(dca.TargetPurchaseCount)),

            IncomeFiatGoalTypeDTO incomeFiat =>
                Result<IGoalType>.Success(new IncomeFiatGoalType(incomeFiat.TargetAmount)),

            IncomeBtcGoalTypeDTO incomeBtc =>
                Result<IGoalType>.Success(new IncomeBtcGoalType(incomeBtc.TargetSats)),

            ReduceExpenseCategoryGoalTypeDTO reduceExpense =>
                await BuildReduceExpenseCategoryGoalTypeAsync(categoryRepository, reduceExpense),

            BitcoinHodlGoalTypeDTO hodl =>
                Result<IGoalType>.Success(new BitcoinHodlGoalType(hodl.MaxSellableSats)),

            SaveFiatGoalTypeDTO saveFiat =>
                Result<IGoalType>.Success(new SaveFiatGoalType(saveFiat.TargetAmount)),

            SavingsRateGoalTypeDTO savingsRate =>
                Result<IGoalType>.Success(new SavingsRateGoalType(savingsRate.TargetPercentage)),

            NetWorthBtcGoalTypeDTO netWorthBtc =>
                Result<IGoalType>.Success(new NetWorthBtcGoalType(netWorthBtc.TargetSats)),

            _ => Result<IGoalType>.Failure("UNKNOWN_GOAL_TYPE", "Unknown goal type")
        };
    }

    public static async Task<Result<IGoalType>> BuildReduceExpenseCategoryGoalTypeAsync(
        ICategoryRepository categoryRepository,
        ReduceExpenseCategoryGoalTypeDTO dto)
    {
        var category = await categoryRepository.GetCategoryByIdAsync(new CategoryId(dto.CategoryId));
        if (category is null)
            return Result<IGoalType>.Failure("CATEGORY_NOT_FOUND", $"Category with id {dto.CategoryId} not found");

        return Result<IGoalType>.Success(
            new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, category.Name.Value));
    }
}
