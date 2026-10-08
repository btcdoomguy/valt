using Valt.App.Kernel;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.DTOs;

namespace Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;

/// <summary>
/// Query to get the transactions contributing to a goal's progress, with a running
/// total that reconciles with the goal's calculated progress.
/// </summary>
public sealed record GetGoalContributingTransactionsQuery : IQuery<Result<GoalContributingTransactionsResult>>
{
    /// <summary>The ID of the goal.</summary>
    public required string GoalId { get; init; }
}
