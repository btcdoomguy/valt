using Valt.App.Kernel;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Validation;
using Valt.Core.Modules.Budget.Categories.Contracts;
using Valt.Core.Modules.Goals;
using Valt.Core.Modules.Goals.Contracts;

namespace Valt.App.Modules.Goals.Commands.CreateGoal;

internal sealed class CreateGoalHandler : ICommandHandler<CreateGoalCommand, CreateGoalResult>
{
    private readonly IGoalRepository _goalRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IValidator<CreateGoalCommand> _validator;

    public CreateGoalHandler(
        IGoalRepository goalRepository,
        ICategoryRepository categoryRepository,
        IValidator<CreateGoalCommand> validator)
    {
        _goalRepository = goalRepository;
        _categoryRepository = categoryRepository;
        _validator = validator;
    }

    public async Task<Result<CreateGoalResult>> HandleAsync(
        CreateGoalCommand command,
        CancellationToken ct = default)
    {
        var validation = _validator.Validate(command);
        if (!validation.IsValid)
            return Result<CreateGoalResult>.Failure(
                new Error("VALIDATION_FAILED", "Validation failed", validation.Errors));

        // Build goal type
        var goalTypeResult = await GoalTypeBuilder.BuildGoalTypeAsync(_categoryRepository, command.GoalType);
        if (goalTypeResult.IsFailure)
            return Result<CreateGoalResult>.Failure(goalTypeResult.Error!);

        var period = (GoalPeriods)command.Period;
        var goal = Goal.New(command.RefDate, period, goalTypeResult.Value!, command.StartDate);

        await _goalRepository.SaveAsync(goal);

        return Result<CreateGoalResult>.Success(new CreateGoalResult(goal.Id.Value));
    }
}
