using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;
using Valt.Infra.Modules.Goals.Queries;

namespace Valt.App.Modules.Goals.Queries.GetStaleGoals;

internal sealed class GetStaleGoalsHandler : IQueryHandler<GetStaleGoalsQuery, IReadOnlyList<StaleGoalDTO>>
{
    private readonly IGoalQueries _goalQueries;

    public GetStaleGoalsHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public async Task<IReadOnlyList<StaleGoalDTO>> HandleAsync(GetStaleGoalsQuery query, CancellationToken ct = default)
    {
        var infraResult = await _goalQueries.GetStaleGoalsAsync();

        return infraResult.Select(g => new StaleGoalDTO(
            g.Id,
            (int)g.TypeName,
            g.GoalTypeJson,
            g.From,
            g.To
        )).ToList();
    }
}
