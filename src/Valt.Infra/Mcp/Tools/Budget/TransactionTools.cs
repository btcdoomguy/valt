using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Transactions.Commands.AddTransaction;
using Valt.App.Modules.Budget.Transactions.Commands.DeleteTransaction;
using Valt.App.Modules.Budget.Transactions.DTOs;
using Valt.App.Modules.Budget.Transactions.Queries.GetTransactions;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools.Budget;

/// <summary>
/// MCP tools for transaction management.
/// </summary>
[McpServerToolType]
public class TransactionTools
{
    /// <summary>
    /// Gets transactions with optional filtering.
    /// </summary>
    [McpServerTool, Description("Get transactions with optional filtering by date, account, category, or search term")]
    public static async Task<TransactionsDTO> GetTransactions(
        IQueryDispatcher dispatcher,
        [Description("Start date filter (format: yyyy-MM-dd)")] string? fromDate = null,
        [Description("End date filter (format: yyyy-MM-dd)")] string? toDate = null,
        [Description("Filter by account IDs (comma-separated)")] string? accountIds = null,
        [Description("Filter by category IDs (comma-separated)")] string? categoryIds = null,
        [Description("Search term for transaction names")] string? searchTerm = null)
    {
        var query = new GetTransactionsQuery
        {
            From = ParseDate(fromDate),
            To = ParseDate(toDate),
            AccountIds = ParseIds(accountIds),
            CategoryIds = ParseIds(categoryIds),
            SearchTerm = searchTerm
        };

        return await dispatcher.DispatchAsync(query);
    }

    /// <summary>
    /// Adds a fiat expense transaction.
    /// </summary>
    [McpServerTool, Description("Add a fiat expense transaction (money leaving a fiat account)")]
    public static async Task<string> AddFiatExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Account ID (the fiat account to debit)")] string accountId,
        [Description("Amount (positive value)")] decimal amount,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new FiatTransactionDto
            {
                FromAccountId = accountId,
                Amount = amount,
                IsCredit = false
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Expense transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Adds a fiat income transaction.
    /// </summary>
    [McpServerTool, Description("Add a fiat income transaction (money entering a fiat account)")]
    public static async Task<string> AddFiatIncome(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Account ID (the fiat account to credit)")] string accountId,
        [Description("Amount (positive value)")] decimal amount,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new FiatTransactionDto
            {
                FromAccountId = accountId,
                Amount = amount,
                IsCredit = true
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Income transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Adds a fiat to fiat transfer transaction.
    /// </summary>
    [McpServerTool, Description("Add a transfer between two fiat accounts")]
    public static async Task<string> AddFiatToFiatTransfer(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Source account ID")] string fromAccountId,
        [Description("Amount leaving source account")] decimal fromAmount,
        [Description("Destination account ID")] string toAccountId,
        [Description("Amount entering destination account")] decimal toAmount,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new FiatToFiatTransferDto
            {
                FromAccountId = fromAccountId,
                FromAmount = fromAmount,
                ToAccountId = toAccountId,
                ToAmount = toAmount
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Transfer transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Adds a Bitcoin expense transaction.
    /// </summary>
    [McpServerTool, Description("Add a Bitcoin expense transaction (sats leaving a BTC account)")]
    public static async Task<string> AddBitcoinExpense(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("BTC Account ID")] string accountId,
        [Description("Amount in satoshis")] long amountSats,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new BitcoinTransactionDto
            {
                FromAccountId = accountId,
                AmountSats = amountSats,
                IsCredit = false
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin expense transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Adds a Bitcoin income transaction.
    /// </summary>
    [McpServerTool, Description("Add a Bitcoin income transaction (sats entering a BTC account)")]
    public static async Task<string> AddBitcoinIncome(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("BTC Account ID")] string accountId,
        [Description("Amount in satoshis")] long amountSats,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new BitcoinTransactionDto
            {
                FromAccountId = accountId,
                AmountSats = amountSats,
                IsCredit = true
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin income transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Adds a fiat to Bitcoin purchase transaction.
    /// </summary>
    [McpServerTool, Description("Add a Bitcoin purchase (fiat leaves, BTC enters)")]
    public static async Task<string> AddBitcoinPurchase(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Transaction date (format: yyyy-MM-dd)")] string date,
        [Description("Transaction name/description")] string name,
        [Description("Category ID")] string categoryId,
        [Description("Fiat account ID (where money leaves)")] string fiatAccountId,
        [Description("Fiat amount spent")] decimal fiatAmount,
        [Description("BTC account ID (where sats arrive)")] string btcAccountId,
        [Description("Satoshis received")] long satsAmount,
        [Description("Optional notes")] string? notes = null)
    {
        var parsedDate = DateOnly.Parse(date);

        var result = await dispatcher.DispatchAsync(new AddTransactionCommand
        {
            Date = parsedDate,
            Name = name,
            CategoryId = categoryId,
            Notes = notes,
            Details = new FiatToBitcoinTransferDto
            {
                FromAccountId = fiatAccountId,
                FromFiatAmount = fiatAmount,
                ToAccountId = btcAccountId,
                ToSatsAmount = satsAmount
            }
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin purchase transaction created with ID: {result.Value.TransactionId}";
    }

    /// <summary>
    /// Deletes a transaction.
    /// </summary>
    [McpServerTool, Description("Delete a transaction")]
    public static async Task<string> DeleteTransaction(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The transaction ID to delete")] string transactionId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteTransactionCommand
        {
            TransactionId = transactionId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Transaction {transactionId} deleted successfully";
    }

    private static DateOnly? ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        return DateOnly.Parse(dateString);
    }

    private static string[]? ParseIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return null;

        return ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
