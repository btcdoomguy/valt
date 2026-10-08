using Valt.App.Kernel;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Contracts;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;

internal sealed class GetGoalContributingTransactionsHandler : IQueryHandler<GetGoalContributingTransactionsQuery, Result<GoalContributingTransactionsResult>>
{
    private readonly IGoalQueries _goalQueries;

    public GetGoalContributingTransactionsHandler(IGoalQueries goalQueries)
    {
        _goalQueries = goalQueries;
    }

    public async Task<Result<GoalContributingTransactionsResult>> HandleAsync(GetGoalContributingTransactionsQuery query, CancellationToken ct = default)
    {
        var result = await _goalQueries.GetContributingTransactionsAsync(query.GoalId);

        return result is null
            ? Result<GoalContributingTransactionsResult>.NotFound("Goal", query.GoalId)
            : Result<GoalContributingTransactionsResult>.Success(result);
    }
}
