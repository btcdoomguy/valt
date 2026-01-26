using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoals;

internal sealed class GetGoalsHandler : IQueryHandler<GetGoalsQuery, IReadOnlyList<GoalDTO>>
{
    private readonly IGoalQueries _goalQueries;

    public GetGoalsHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public Task<IReadOnlyList<GoalDTO>> HandleAsync(GetGoalsQuery query, CancellationToken ct = default)
    {
        return _goalQueries.GetGoalsAsync(query.FilterDate);
    }
}
