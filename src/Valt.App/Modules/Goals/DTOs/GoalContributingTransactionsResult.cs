using Valt.Core.Modules.Goals;

namespace Valt.App.Modules.Goals.DTOs;

/// <summary>
/// Result of the contributing-transactions query: either the ordered rows for a
/// transaction-based goal type, or a typed NotSupported marker for goal types whose
/// progress is not derived from transactions.
/// </summary>
public abstract record GoalContributingTransactionsResult
{
    /// <summary>The goal type exposes its contributing transactions.</summary>
    public sealed record Supported(IReadOnlyList<ContributingTransactionRow> Rows) : GoalContributingTransactionsResult;

    /// <summary>The goal type's progress is not derived from transactions.</summary>
    public sealed record NotSupported(GoalTypeNames Type) : GoalContributingTransactionsResult;
}
