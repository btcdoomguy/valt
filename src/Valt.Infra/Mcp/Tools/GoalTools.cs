using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Commands.CreateGoal;
using Valt.App.Modules.Goals.Commands.DeleteGoal;
using Valt.App.Modules.Goals.Commands.EditGoal;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoal;
using Valt.App.Modules.Goals.Queries.GetGoalContributingTransactions;
using Valt.App.Modules.Goals.Queries.GetGoals;
using Valt.Core.Modules.Goals;
using Valt.App.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools;

/// <summary>
/// MCP tools for financial goal management.
/// </summary>
[McpServerToolType]
public class GoalTools
{
    /// <summary>
    /// Gets all goals, optionally filtered by date.
    /// </summary>
    [McpServerTool, Description("Get all financial goals, optionally filtered to goals containing a specific date")]
    public static async Task<IReadOnlyList<GoalDTO>> GetGoals(
        IQueryDispatcher dispatcher,
        [Description("Optional filter date (format: yyyy-MM-dd) - returns only goals containing this date")] string? filterDate = null)
    {
        DateOnly? parsedDate = string.IsNullOrWhiteSpace(filterDate)
            ? null
            : DateOnly.Parse(filterDate);

        return await dispatcher.DispatchAsync(new GetGoalsQuery { FilterDate = parsedDate });
    }

    /// <summary>
    /// Gets a single goal by ID.
    /// </summary>
    [McpServerTool, Description("Get a single financial goal by its ID")]
    public static async Task<GoalDTO?> GetGoal(
        IQueryDispatcher dispatcher,
        [Description("The goal ID")] string goalId)
    {
        return await dispatcher.DispatchAsync(new GetGoalQuery { GoalId = goalId });
    }

    /// <summary>
    /// Gets the transactions contributing to a goal's progress, with running total.
    /// Returns null only when the goal is not found. Returns a typed result with
    /// <see cref="GoalContributingTransactionsMcpResult.Supported"/> = false for goal
    /// types whose progress is not derived from transactions (e.g. NetWorthBtc), and
    /// also with <see cref="GoalContributingTransactionsMcpResult.Error"/> set when the
    /// underlying query fails — so "not supported" and "internal failure" are never
    /// conflated.
    /// </summary>
    [McpServerTool, Description("Get the transactions contributing to a goal's progress, with running total")]
    public static async Task<GoalContributingTransactionsMcpResult?> GetGoalContributingTransactions(
        IQueryDispatcher dispatcher,
        [Description("ID of the goal (from GetGoals)")] string goalId)
    {
        var goal = await dispatcher.DispatchAsync(new GetGoalQuery { GoalId = goalId });
        if (goal is null)
            return null;

        var goalType = ((GoalTypeNames)goal.GoalType.TypeId).ToString();

        var result = await dispatcher.DispatchAsync(new GetGoalContributingTransactionsQuery { GoalId = goalId });
        if (result.IsFailure)
        {
            return new GoalContributingTransactionsMcpResult
            {
                Supported = false,
                GoalType = goalType,
                StrategyUnit = null,
                FinalTotal = null,
                Rows = [],
                Error = result.Error?.Message ?? "Unknown error"
            };
        }

        if (result.Value is GoalContributingTransactionsResult.NotSupported)
        {
            return new GoalContributingTransactionsMcpResult
            {
                Supported = false,
                GoalType = goalType,
                StrategyUnit = null,
                FinalTotal = null,
                Rows = []
            };
        }

        var rows = ((GoalContributingTransactionsResult.Supported)result.Value!).Rows;
        var mapped = new List<GoalContributingTransactionMcpRow>(rows.Count);
        decimal previousRunningTotal = 0m;
        foreach (var row in rows)
        {
            var contribution = mapped.Count == 0
                ? row.RunningTotal
                : row.RunningTotal - previousRunningTotal;
            previousRunningTotal = row.RunningTotal;

            mapped.Add(new GoalContributingTransactionMcpRow
            {
                Date = row.Date,
                Description = row.Description,
                Account = row.AccountName,
                Category = row.CategoryName,
                FiatAmount = row.FiatAmount.Value,
                FiatCurrencyCode = row.FiatCurrencyCode,
                SatsAmount = row.SatsAmount.Sats,
                Contribution = contribution,
                RunningTotal = row.RunningTotal
            });
        }

        return new GoalContributingTransactionsMcpResult
        {
            Supported = true,
            GoalType = goalType,
            StrategyUnit = GetStrategyUnit(goal.GoalType.TypeId),
            FinalTotal = rows.Count > 0 ? rows[^1].RunningTotal : null,
            Rows = mapped
        };
    }

    /// <summary>
    /// Maps a goal type to the unit its running total is expressed in
    /// (mirrors the SummaryStrategyUnit semantics in the UI layer).
    /// </summary>
    private static string GetStrategyUnit(int typeId) => (GoalTypeNames)typeId switch
    {
        GoalTypeNames.StackBitcoin or GoalTypeNames.IncomeBtc or GoalTypeNames.BitcoinHodl => "Sats",
        GoalTypeNames.Dca => "Count",
        GoalTypeNames.SavingsRate => "Percentage",
        _ => "Fiat" // SpendingLimit, IncomeFiat, ReduceExpenseCategory, SaveFiat (NetWorthBtc never reaches here — it takes the NotSupported path above)
    };

    /// <summary>
    /// Creates a Stack Bitcoin goal (accumulate a target amount of sats).
    /// </summary>
    [McpServerTool, Description("Create a goal to stack a target amount of Bitcoin (sats)")]
    public static async Task<string> CreateStackBitcoinGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target amount in satoshis")] long targetSats,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new StackBitcoinGoalTypeDTO { TargetSats = targetSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Stack Bitcoin goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Spending Limit goal (stay under a fiat spending amount).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit fiat spending to a maximum amount")]
    public static async Task<string> CreateSpendingLimitGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Maximum spending amount")] decimal targetAmount,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new SpendingLimitGoalTypeDTO { TargetAmount = targetAmount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Spending Limit goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a DCA (Dollar Cost Averaging) goal (make a target number of BTC purchases).
    /// </summary>
    [McpServerTool, Description("Create a DCA goal to make a target number of Bitcoin purchases")]
    public static async Task<string> CreateDcaGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target number of purchases")] int targetPurchaseCount,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new DcaGoalTypeDTO { TargetPurchaseCount = targetPurchaseCount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"DCA goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Fiat Income goal (earn a target fiat amount).
    /// </summary>
    [McpServerTool, Description("Create a goal to earn a target amount of fiat income")]
    public static async Task<string> CreateIncomeFiatGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target income amount")] decimal targetAmount,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new IncomeFiatGoalTypeDTO { TargetAmount = targetAmount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Fiat Income goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Bitcoin Income goal (earn a target amount of sats).
    /// </summary>
    [McpServerTool, Description("Create a goal to earn a target amount of Bitcoin income (sats)")]
    public static async Task<string> CreateIncomeBtcGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target income in satoshis")] long targetSats,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new IncomeBtcGoalTypeDTO { TargetSats = targetSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin Income goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Reduce Expense Category goal (reduce spending in a specific category).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit spending in a specific category")]
    public static async Task<string> CreateReduceExpenseCategoryGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Category ID to track")] string categoryId,
        [Description("Maximum spending amount for the category")] decimal targetAmount,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new ReduceExpenseCategoryGoalTypeDTO
            {
                CategoryId = categoryId,
                TargetAmount = targetAmount
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Reduce Expense Category goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Bitcoin HODL goal (limit BTC selling).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit Bitcoin selling (HODL goal)")]
    public static async Task<string> CreateBitcoinHodlGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Maximum sats that can be sold (0 = no selling allowed)")] long maxSellableSats,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new BitcoinHodlGoalTypeDTO { MaxSellableSats = maxSellableSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin HODL goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Save Fiat goal (save a target fiat amount from income minus expenses).
    /// </summary>
    [McpServerTool, Description("Create a goal to save a target fiat amount (income minus expenses)")]
    public static async Task<string> CreateSaveFiatGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target savings amount")] decimal targetAmount,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new SaveFiatGoalTypeDTO { TargetAmount = targetAmount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Save Fiat goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Savings Rate goal (save a target percentage of income).
    /// </summary>
    [McpServerTool, Description("Create a goal to save a target percentage of income")]
    public static async Task<string> CreateSavingsRateGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target savings rate percentage (1-100)")] decimal targetPercentage,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new SavingsRateGoalTypeDTO { TargetPercentage = targetPercentage }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Savings Rate goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Net Worth BTC goal (reach a target net worth in sats).
    /// </summary>
    [McpServerTool, Description("Create a goal to reach a target net worth in bitcoin (sats)")]
    public static async Task<string> CreateNetWorthBtcGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target net worth in satoshis")] long targetSats,
        [Description("Optional start date for yearly goals (format: yyyy-MM-dd). Must be in the same year as refDate.")] string? startDate = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            StartDate = string.IsNullOrWhiteSpace(startDate) ? null : DateOnly.Parse(startDate),
            GoalType = new NetWorthBtcGoalTypeDTO { TargetSats = targetSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Net Worth BTC goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Deletes a goal.
    /// </summary>
    [McpServerTool, Description("Delete a financial goal")]
    public static async Task<string> DeleteGoal(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The goal ID to delete")] string goalId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteGoalCommand
        {
            GoalId = goalId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Goal {goalId} deleted successfully";
    }
}

/// <summary>
/// Flat result of the contributing-transactions query for MCP clients.
/// <see cref="Supported"/> is false (typed, not an error) for goal types whose
/// progress is not derived from transactions; <see cref="Error"/> is set only when
/// the underlying query failed, so clients can distinguish "not supported" from
/// "internal failure".
/// </summary>
public sealed record GoalContributingTransactionsMcpResult
{
    /// <summary>True when rows are available; false for non-transaction-based goal types or on internal failure.</summary>
    public required bool Supported { get; init; }

    /// <summary>Goal type enum name (e.g. StackBitcoin, NetWorthBtc).</summary>
    public required string? GoalType { get; init; }

    /// <summary>Unit of Contribution/RunningTotal/FinalTotal: Fiat, Sats, Count, or Percentage. Null when not supported.</summary>
    public required string? StrategyUnit { get; init; }

    /// <summary>Final running total, equal to the goal's calculated field. Null when not supported or no rows.</summary>
    public required decimal? FinalTotal { get; init; }

    /// <summary>Contributing transactions in date order, each with contribution and running total.</summary>
    public required IReadOnlyList<GoalContributingTransactionMcpRow> Rows { get; init; }

    /// <summary>Error message when the underlying query failed (Supported=false). Null on success and for typed NotSupported results.</summary>
    public string? Error { get; init; }
}

/// <summary>A single contributing transaction row for MCP clients.</summary>
public sealed record GoalContributingTransactionMcpRow
{
    /// <summary>Date of the contributing transaction.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Transaction description (name).</summary>
    public required string Description { get; init; }

    /// <summary>Name of the account the amount left (or entered, when no origin account exists).</summary>
    public required string Account { get; init; }

    /// <summary>Category name, or null when the transaction is uncategorized.</summary>
    public required string? Category { get; init; }

    /// <summary>Fiat amount magnitude (always non-negative), in <see cref="FiatCurrencyCode"/>.</summary>
    public required decimal FiatAmount { get; init; }

    /// <summary>ISO code of the currency <see cref="FiatAmount"/> is expressed in.</summary>
    public required string FiatCurrencyCode { get; init; }

    /// <summary>Sats amount magnitude (always non-negative), converted at the transaction-date BTC price.</summary>
    public required long SatsAmount { get; init; }

    /// <summary>Contribution of this row to the running total: first row = its RunningTotal, later rows = delta.</summary>
    public required decimal Contribution { get; init; }

    /// <summary>Cumulative total in the strategy's own unit after this row.</summary>
    public required decimal RunningTotal { get; init; }
}
