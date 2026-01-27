using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Accounts.Commands.CreateBtcAccount;
using Valt.App.Modules.Budget.Accounts.Commands.CreateFiatAccount;
using Valt.App.Modules.Budget.Accounts.Commands.DeleteAccount;
using Valt.App.Modules.Budget.Accounts.Commands.EditAccount;
using Valt.App.Modules.Budget.Accounts.DTOs;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccount;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccountGroups;
using Valt.App.Modules.Budget.Accounts.Queries.GetAccounts;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools.Budget;

/// <summary>
/// MCP tools for account management.
/// </summary>
[McpServerToolType]
public class AccountTools
{
    /// <summary>
    /// Gets all accounts with their balances.
    /// </summary>
    [McpServerTool, Description("Get all accounts with their current balances")]
    public static async Task<IReadOnlyList<AccountDTO>> GetAccounts(
        IQueryDispatcher dispatcher,
        [Description("Include hidden accounts in the result")] bool includeHidden = false)
    {
        return await dispatcher.DispatchAsync(new GetAccountsQuery(includeHidden));
    }

    /// <summary>
    /// Gets a single account by ID.
    /// </summary>
    [McpServerTool, Description("Get a single account by its ID")]
    public static async Task<AccountDTO?> GetAccount(
        IQueryDispatcher dispatcher,
        [Description("The account ID")] string accountId)
    {
        return await dispatcher.DispatchAsync(new GetAccountQuery { AccountId = accountId });
    }

    /// <summary>
    /// Gets all account groups.
    /// </summary>
    [McpServerTool, Description("Get all account groups")]
    public static async Task<IReadOnlyList<AccountGroupDTO>> GetAccountGroups(
        IQueryDispatcher dispatcher)
    {
        return await dispatcher.DispatchAsync(new GetAccountGroupsQuery());
    }

    /// <summary>
    /// Creates a new fiat currency account.
    /// </summary>
    [McpServerTool, Description("Create a new fiat currency account (e.g., bank account, credit card)")]
    public static async Task<string> CreateFiatAccount(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Account name")] string name,
        [Description("Currency code (e.g., USD, EUR, BRL)")] string currency,
        [Description("Icon ID for the account")] string iconId,
        [Description("Initial balance amount")] decimal initialAmount = 0,
        [Description("Optional currency nickname")] string? currencyNickname = null,
        [Description("Whether the account is visible")] bool visible = true,
        [Description("Optional group ID to assign the account to")] string? groupId = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateFiatAccountCommand
        {
            Name = name,
            Currency = currency,
            IconId = iconId,
            InitialAmount = initialAmount,
            CurrencyNickname = currencyNickname,
            Visible = visible,
            GroupId = groupId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Account created with ID: {result.Value.AccountId}";
    }

    /// <summary>
    /// Creates a new Bitcoin account.
    /// </summary>
    [McpServerTool, Description("Create a new Bitcoin account (e.g., cold storage, exchange wallet)")]
    public static async Task<string> CreateBtcAccount(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Account name")] string name,
        [Description("Icon ID for the account")] string iconId,
        [Description("Initial balance in satoshis")] long initialAmountSats = 0,
        [Description("Optional currency nickname")] string? currencyNickname = null,
        [Description("Whether the account is visible")] bool visible = true,
        [Description("Optional group ID to assign the account to")] string? groupId = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateBtcAccountCommand
        {
            Name = name,
            IconId = iconId,
            InitialAmountSats = initialAmountSats,
            CurrencyNickname = currencyNickname,
            Visible = visible,
            GroupId = groupId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Bitcoin account created with ID: {result.Value.AccountId}";
    }

    /// <summary>
    /// Edits an existing account.
    /// </summary>
    [McpServerTool, Description("Edit an existing account's properties (all fields are required)")]
    public static async Task<string> EditAccount(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The account ID to edit")] string accountId,
        [Description("Account name")] string name,
        [Description("Icon ID")] string iconId,
        [Description("Whether the account is visible")] bool visible,
        [Description("Currency nickname (optional)")] string? currencyNickname = null,
        [Description("Group ID (optional, use empty string to remove from group)")] string? groupId = null,
        [Description("Currency code (for fiat accounts only)")] string? currency = null,
        [Description("Initial fiat amount (for fiat accounts only)")] decimal? initialAmountFiat = null,
        [Description("Initial satoshi amount (for BTC accounts only)")] long? initialAmountSats = null)
    {
        var result = await dispatcher.DispatchAsync(new EditAccountCommand
        {
            AccountId = accountId,
            Name = name,
            CurrencyNickname = currencyNickname,
            Visible = visible,
            IconId = iconId,
            GroupId = groupId,
            Currency = currency,
            InitialAmountFiat = initialAmountFiat,
            InitialAmountSats = initialAmountSats
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Account {accountId} updated successfully";
    }

    /// <summary>
    /// Deletes an account.
    /// </summary>
    [McpServerTool, Description("Delete an account (will fail if it has transactions)")]
    public static async Task<string> DeleteAccount(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The account ID to delete")] string accountId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteAccountCommand
        {
            AccountId = accountId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Account {accountId} deleted successfully";
    }
}
