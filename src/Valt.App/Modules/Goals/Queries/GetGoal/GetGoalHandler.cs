using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoal;

internal sealed class GetGoalHandler : IQueryHandler<GetGoalQuery, GoalDTO?>
{
    private readonly IGoalQueries _goalQueries;

    public GetGoalHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public Task<GoalDTO?> HandleAsync(GetGoalQuery query, CancellationToken ct = default)
    {
        return _goalQueries.GetGoalAsync(query.GoalId);
    }
}
