using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Goals.Commands.CreateGoal;
using Valt.App.Modules.Goals.Commands.DeleteGoal;
using Valt.App.Modules.Goals.Commands.EditGoal;
using Valt.App.Modules.Goals.DTOs;
using Valt.App.Modules.Goals.Queries.GetGoal;
using Valt.App.Modules.Goals.Queries.GetGoals;

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
    /// Creates a Stack Bitcoin goal (accumulate a target amount of sats).
    /// </summary>
    [McpServerTool, Description("Create a goal to stack a target amount of Bitcoin (sats)")]
    public static async Task<string> CreateStackBitcoinGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target amount in satoshis")] long targetSats)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new StackBitcoinGoalTypeDTO { TargetSats = targetSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"Stack Bitcoin goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Spending Limit goal (stay under a fiat spending amount).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit fiat spending to a maximum amount")]
    public static async Task<string> CreateSpendingLimitGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Maximum spending amount")] decimal targetAmount)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new SpendingLimitGoalTypeDTO { TargetAmount = targetAmount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"Spending Limit goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a DCA (Dollar Cost Averaging) goal (make a target number of BTC purchases).
    /// </summary>
    [McpServerTool, Description("Create a DCA goal to make a target number of Bitcoin purchases")]
    public static async Task<string> CreateDcaGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target number of purchases")] int targetPurchaseCount)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new DcaGoalTypeDTO { TargetPurchaseCount = targetPurchaseCount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"DCA goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Fiat Income goal (earn a target fiat amount).
    /// </summary>
    [McpServerTool, Description("Create a goal to earn a target amount of fiat income")]
    public static async Task<string> CreateIncomeFiatGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target income amount")] decimal targetAmount)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new IncomeFiatGoalTypeDTO { TargetAmount = targetAmount }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"Fiat Income goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Bitcoin Income goal (earn a target amount of sats).
    /// </summary>
    [McpServerTool, Description("Create a goal to earn a target amount of Bitcoin income (sats)")]
    public static async Task<string> CreateIncomeBtcGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Target income in satoshis")] long targetSats)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new IncomeBtcGoalTypeDTO { TargetSats = targetSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"Bitcoin Income goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Reduce Expense Category goal (reduce spending in a specific category).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit spending in a specific category")]
    public static async Task<string> CreateReduceExpenseCategoryGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Category ID to track")] string categoryId,
        [Description("Maximum spending amount for the category")] decimal targetAmount)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
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

        return $"Reduce Expense Category goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Creates a Bitcoin HODL goal (limit BTC selling).
    /// </summary>
    [McpServerTool, Description("Create a goal to limit Bitcoin selling (HODL goal)")]
    public static async Task<string> CreateBitcoinHodlGoal(
        ICommandDispatcher dispatcher,
        [Description("Reference date (format: yyyy-MM-dd)")] string refDate,
        [Description("Period type: 0=Monthly, 1=Yearly")] int period,
        [Description("Maximum sats that can be sold (0 = no selling allowed)")] long maxSellableSats)
    {
        var result = await dispatcher.DispatchAsync(new CreateGoalCommand
        {
            RefDate = DateOnly.Parse(refDate),
            Period = period,
            GoalType = new BitcoinHodlGoalTypeDTO { MaxSellableSats = maxSellableSats }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        return $"Bitcoin HODL goal created with ID: {result.Value.GoalId}";
    }

    /// <summary>
    /// Deletes a goal.
    /// </summary>
    [McpServerTool, Description("Delete a financial goal")]
    public static async Task<string> DeleteGoal(
        ICommandDispatcher dispatcher,
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

        return $"Goal {goalId} deleted successfully";
    }
}
