using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.FixedExpenses.Commands.CreateFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.Commands.DeleteFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.Commands.EditFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.DTOs;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpense;
using Valt.App.Modules.Budget.FixedExpenses.Queries.GetFixedExpenses;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools.Budget;

/// <summary>
/// MCP tools for fixed expense management.
/// </summary>
[McpServerToolType]
public class FixedExpenseTools
{
    /// <summary>
    /// Gets all fixed expenses.
    /// </summary>
    [McpServerTool, Description("Get all fixed/recurring expenses")]
    public static async Task<IReadOnlyList<FixedExpenseDTO>> GetFixedExpenses(
        IQueryDispatcher dispatcher)
    {
        return await dispatcher.DispatchAsync(new GetFixedExpensesQuery());
    }

    /// <summary>
    /// Gets a single fixed expense by ID.
    /// </summary>
    [McpServerTool, Description("Get a single fixed expense by its ID")]
    public static async Task<FixedExpenseDTO?> GetFixedExpense(
        IQueryDispatcher dispatcher,
        [Description("The fixed expense ID")] string fixedExpenseId)
    {
        return await dispatcher.DispatchAsync(new GetFixedExpenseQuery { FixedExpenseId = fixedExpenseId });
    }

    /// <summary>
    /// Creates a new fixed expense with a fixed amount (monthly).
    /// </summary>
    [McpServerTool, Description("Create a new monthly fixed expense with a constant amount")]
    public static async Task<string> CreateMonthlyFixedExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Expense name")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Fixed amount value")] decimal amount,
        [Description("Day of month (1-28) when the expense is due")] int dayOfMonth,
        [Description("Start period date (format: yyyy-MM-dd)")] string startPeriod,
        [Description("Default account ID (optional)")] string? defaultAccountId = null,
        [Description("Currency code if not using account currency (optional, e.g., USD)")] string? currency = null,
        [Description("Whether the expense is enabled")] bool enabled = true)
    {
        var result = await dispatcher.DispatchAsync(new CreateFixedExpenseCommand
        {
            Name = name,
            CategoryId = categoryId,
            DefaultAccountId = defaultAccountId,
            Currency = currency,
            Enabled = enabled,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.Parse(startPeriod),
                    FixedAmount = amount,
                    PeriodId = 0, // Monthly
                    Day = dayOfMonth
                }
            ]
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Fixed expense created with ID: {result.Value.FixedExpenseId}";
    }

    /// <summary>
    /// Creates a new fixed expense with a variable amount range (monthly).
    /// </summary>
    [McpServerTool, Description("Create a new monthly fixed expense with a variable amount range")]
    public static async Task<string> CreateMonthlyVariableExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Expense name")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Minimum expected amount")] decimal minAmount,
        [Description("Maximum expected amount")] decimal maxAmount,
        [Description("Day of month (1-28) when the expense is due")] int dayOfMonth,
        [Description("Start period date (format: yyyy-MM-dd)")] string startPeriod,
        [Description("Default account ID (optional)")] string? defaultAccountId = null,
        [Description("Currency code if not using account currency (optional, e.g., USD)")] string? currency = null,
        [Description("Whether the expense is enabled")] bool enabled = true)
    {
        var result = await dispatcher.DispatchAsync(new CreateFixedExpenseCommand
        {
            Name = name,
            CategoryId = categoryId,
            DefaultAccountId = defaultAccountId,
            Currency = currency,
            Enabled = enabled,
            Ranges =
            [
                new FixedExpenseRangeInputDTO
                {
                    PeriodStart = DateOnly.Parse(startPeriod),
                    RangedAmountMin = minAmount,
                    RangedAmountMax = maxAmount,
                    PeriodId = 0, // Monthly
                    Day = dayOfMonth
                }
            ]
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Fixed expense created with ID: {result.Value.FixedExpenseId}";
    }

    /// <summary>
    /// Edits an existing fixed expense's basic properties.
    /// </summary>
    [McpServerTool, Description("Edit an existing fixed expense's name, category, and enabled status")]
    public static async Task<string> EditFixedExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The fixed expense ID to edit")] string fixedExpenseId,
        [Description("New expense name")] string name,
        [Description("New category ID")] string categoryId,
        [Description("Whether the expense is enabled")] bool enabled)
    {
        var result = await dispatcher.DispatchAsync(new EditFixedExpenseCommand
        {
            FixedExpenseId = fixedExpenseId,
            Name = name,
            CategoryId = categoryId,
            Enabled = enabled
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Fixed expense {fixedExpenseId} updated successfully";
    }

    /// <summary>
    /// Deletes a fixed expense.
    /// </summary>
    [McpServerTool, Description("Delete a fixed expense (transactions will lose their association)")]
    public static async Task<string> DeleteFixedExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The fixed expense ID to delete")] string fixedExpenseId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteFixedExpenseCommand
        {
            FixedExpenseId = fixedExpenseId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Fixed expense {fixedExpenseId} deleted successfully";
    }
}
