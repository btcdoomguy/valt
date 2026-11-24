using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;

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
        var goalTypeResult = await GoalTypeBuilder.BuildGoalTypeAsync(_categoryRepository, command.GoalType);
        if (goalTypeResult.IsFailure)
            return Result<EditGoalResult>.Failure(goalTypeResult.Error!);

        var period = (GoalPeriods)command.Period;
        goal.Edit(command.RefDate, period, goalTypeResult.Value!, command.StartDate);

        await _goalRepository.SaveAsync(goal);

        return Result<EditGoalResult>.Success(new EditGoalResult());
    }
}
