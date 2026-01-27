using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetStaleGoals;

internal sealed class GetStaleGoalsHandler : IQueryHandler<GetStaleGoalsQuery, IReadOnlyList<StaleGoalDTO>>
{
    private readonly IGoalQueries _goalQueries;

    public GetStaleGoalsHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public Task<IReadOnlyList<StaleGoalDTO>> HandleAsync(GetStaleGoalsQuery query, CancellationToken ct = default)
    {
        return _goalQueries.GetStaleGoalsAsync();
    }
}
