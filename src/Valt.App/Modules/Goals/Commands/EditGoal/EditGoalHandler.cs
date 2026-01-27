using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.App.Modules.Goals.DTOs;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;
using Valt.Core.Modules.Goals.GoalTypes;

namespace Valt.App.Modules.Goals.Commands.EditGoal;

internal sealed class EditGoalHandler : ICommandHandler<EditGoalCommand, EditGoalResult>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IValidator<EditGoalCommand> _validator;

    public EditGoalHandler(
        IGoalRepository goalRepository,
        ICategoryRepository categoryRepository,
        IValidator<EditGoalCommand> validator)
    {
        _goalRepository = goalRepository;
        _categoryRepository = categoryRepository;
        _validator = validator;
    }

    public async Task<Result<EditGoalResult>> HandleAsync(
        EditGoalCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<EditGoalResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        var goal = await _goalRepository.GetByIdAsync(new GoalId(command.GoalId));
        if (goal is null)
            return Result<EditGoalResult>.Failure(
                "GOAL_NOT_FOUND", $"Goal with id {command.GoalId} not found");

        // Build goal type
        var goalTypeResult = await BuildGoalTypeAsync(command.GoalType);
        if (goalTypeResult.IsFailure)
            return Result<EditGoalResult>.Failure(goalTypeResult.Error!);

        var period = (GoalPeriods)command.Period;
        goal.Edit(command.RefDate, period, goalTypeResult.Value!);

        await _goalRepository.SaveAsync(goal);

        return Result<EditGoalResult>.Success(new EditGoalResult());
    }

    private async Task<Result<IGoalType>> BuildGoalTypeAsync(GoalTypeInputDTO dto)
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
                await BuildReduceExpenseCategoryGoalTypeAsync(reduceExpense),

            BitcoinHodlGoalTypeDTO hodl =>
                Result<IGoalType>.Success(new BitcoinHodlGoalType(hodl.MaxSellableSats)),

            _ => Result<IGoalType>.Failure("UNKNOWN_GOAL_TYPE", "Unknown goal type")
        };
    }

    private async Task<Result<IGoalType>> BuildReduceExpenseCategoryGoalTypeAsync(ReduceExpenseCategoryGoalTypeDTO dto)
    {
        var category = await _categoryRepository.GetCategoryByIdAsync(new CategoryId(dto.CategoryId));
        if (category is null)
            return Result<IGoalType>.Failure("CATEGORY_NOT_FOUND", $"Category with id {dto.CategoryId} not found");

        return Result<IGoalType>.Success(
            new ReduceExpenseCategoryGoalType(dto.TargetAmount, dto.CategoryId, category.Name.Value));
    }
}
