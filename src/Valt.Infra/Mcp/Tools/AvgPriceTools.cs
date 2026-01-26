using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.AvgPrice.Commands.AddLine;
using Valt.App.Modules.AvgPrice.Commands.CreateProfile;
using Valt.App.Modules.AvgPrice.Commands.DeleteLine;
using Valt.App.Modules.AvgPrice.Commands.DeleteProfile;
using Valt.App.Modules.AvgPrice.Commands.EditLine;
using Valt.App.Modules.AvgPrice.Commands.EditProfile;
using Valt.App.Modules.AvgPrice.DTOs;
using Valt.App.Modules.AvgPrice.Queries.GetLinesOfProfile;
using Valt.App.Modules.AvgPrice.Queries.GetProfile;
using Valt.App.Modules.AvgPrice.Queries.GetProfiles;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools;

/// <summary>
/// MCP tools for average price / cost basis tracking.
/// </summary>
[McpServerToolType]
public class AvgPriceTools
{
    /// <summary>
    /// Gets all average price profiles.
    /// </summary>
    [McpServerTool, Description("Get all average price/cost basis tracking profiles")]
    public static async Task<IReadOnlyList<AvgPriceProfileDTO>> GetProfiles(
        IQueryDispatcher dispatcher,
        [Description("Include hidden profiles")] bool showHidden = false)
    {
        return await dispatcher.DispatchAsync(new GetProfilesQuery { ShowHidden = showHidden });
    }

    /// <summary>
    /// Gets a single profile by ID.
    /// </summary>
    [McpServerTool, Description("Get a single average price profile by its ID")]
    public static async Task<AvgPriceProfileDTO?> GetProfile(
        IQueryDispatcher dispatcher,
        [Description("The profile ID")] string profileId)
    {
        return await dispatcher.DispatchAsync(new GetProfileQuery { ProfileId = profileId });
    }

    /// <summary>
    /// Gets all lines (buy/sell entries) for a profile.
    /// </summary>
    [McpServerTool, Description("Get all buy/sell/setup lines for an average price profile")]
    public static async Task<IReadOnlyList<AvgPriceLineDTO>> GetProfileLines(
        IQueryDispatcher dispatcher,
        [Description("The profile ID")] string profileId)
    {
        return await dispatcher.DispatchAsync(new GetLinesOfProfileQuery { ProfileId = profileId });
    }

    /// <summary>
    /// Creates a new average price profile with Brazilian Rule calculation.
    /// </summary>
    [McpServerTool, Description("Create a new average price profile using Brazilian Rule calculation (weighted average)")]
    public static async Task<string> CreateBrazilianRuleProfile(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Profile name")] string name,
        [Description("Asset name (e.g., 'Bitcoin', 'BTC')")] string assetName,
        [Description("Decimal precision for amounts (e.g., 8 for satoshis)")] int precision,
        [Description("Currency code (e.g., 'BRL', 'USD')")] string currencyCode,
        [Description("Whether the profile is visible")] bool visible = true,
        [Description("Icon name (optional)")] string? iconName = null,
        [Description("Icon unicode character (optional)")] char iconUnicode = '\0',
        [Description("Icon color as integer (optional)")] int iconColor = 0)
    {
        var result = await dispatcher.DispatchAsync(new CreateProfileCommand
        {
            Name = name,
            AssetName = assetName,
            Precision = precision,
            Visible = visible,
            IconName = iconName,
            IconUnicode = iconUnicode,
            IconColor = iconColor,
            CurrencyCode = currencyCode,
            CalculationMethodId = 0 // BrazilianRule
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Profile created with ID: {result.Value.ProfileId}";
    }

    /// <summary>
    /// Creates a new average price profile with FIFO calculation.
    /// </summary>
    [McpServerTool, Description("Create a new average price profile using FIFO calculation (First-In-First-Out)")]
    public static async Task<string> CreateFifoProfile(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Profile name")] string name,
        [Description("Asset name (e.g., 'Bitcoin', 'BTC')")] string assetName,
        [Description("Decimal precision for amounts (e.g., 8 for satoshis)")] int precision,
        [Description("Currency code (e.g., 'BRL', 'USD')")] string currencyCode,
        [Description("Whether the profile is visible")] bool visible = true,
        [Description("Icon name (optional)")] string? iconName = null,
        [Description("Icon unicode character (optional)")] char iconUnicode = '\0',
        [Description("Icon color as integer (optional)")] int iconColor = 0)
    {
        var result = await dispatcher.DispatchAsync(new CreateProfileCommand
        {
            Name = name,
            AssetName = assetName,
            Precision = precision,
            Visible = visible,
            IconName = iconName,
            IconUnicode = iconUnicode,
            IconColor = iconColor,
            CurrencyCode = currencyCode,
            CalculationMethodId = 1 // FIFO
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Profile created with ID: {result.Value.ProfileId}";
    }

    /// <summary>
    /// Edits an existing profile.
    /// </summary>
    [McpServerTool, Description("Edit an average price profile")]
    public static async Task<string> EditProfile(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID to edit")] string profileId,
        [Description("Profile name")] string name,
        [Description("Asset name")] string assetName,
        [Description("Decimal precision")] int precision,
        [Description("Calculation method: 0=BrazilianRule, 1=FIFO")] int calculationMethodId,
        [Description("Whether the profile is visible")] bool visible = true,
        [Description("Icon name (optional)")] string? iconName = null,
        [Description("Icon unicode character (optional)")] char iconUnicode = '\0',
        [Description("Icon color as integer (optional)")] int iconColor = 0)
    {
        var result = await dispatcher.DispatchAsync(new EditProfileCommand
        {
            ProfileId = profileId,
            Name = name,
            AssetName = assetName,
            Precision = precision,
            Visible = visible,
            IconName = iconName,
            IconUnicode = iconUnicode,
            IconColor = iconColor,
            CalculationMethodId = calculationMethodId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Profile {profileId} updated successfully";
    }

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    [McpServerTool, Description("Delete an average price profile and all its lines")]
    public static async Task<string> DeleteProfile(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID to delete")] string profileId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteProfileCommand
        {
            ProfileId = profileId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Profile {profileId} deleted successfully";
    }

    /// <summary>
    /// Adds a buy line to a profile.
    /// </summary>
    [McpServerTool, Description("Add a buy (acquisition) line to an average price profile")]
    public static async Task<string> AddBuyLine(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID")] string profileId,
        [Description("Date of acquisition (format: yyyy-MM-dd)")] string date,
        [Description("Quantity acquired")] decimal quantity,
        [Description("Total amount paid")] decimal amount,
        [Description("Optional comment")] string? comment = null)
    {
        var result = await dispatcher.DispatchAsync(new AddLineCommand
        {
            ProfileId = profileId,
            Date = DateOnly.Parse(date),
            LineTypeId = 0, // Buy
            Quantity = quantity,
            Amount = amount,
            Comment = comment
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Buy line added with ID: {result.Value.LineId}";
    }

    /// <summary>
    /// Adds a sell line to a profile.
    /// </summary>
    [McpServerTool, Description("Add a sell (disposal) line to an average price profile")]
    public static async Task<string> AddSellLine(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID")] string profileId,
        [Description("Date of sale (format: yyyy-MM-dd)")] string date,
        [Description("Quantity sold")] decimal quantity,
        [Description("Total amount received")] decimal amount,
        [Description("Optional comment")] string? comment = null)
    {
        var result = await dispatcher.DispatchAsync(new AddLineCommand
        {
            ProfileId = profileId,
            Date = DateOnly.Parse(date),
            LineTypeId = 1, // Sell
            Quantity = quantity,
            Amount = amount,
            Comment = comment
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Sell line added with ID: {result.Value.LineId}";
    }

    /// <summary>
    /// Adds a setup line to a profile (initial position).
    /// </summary>
    [McpServerTool, Description("Add a setup line (initial position) to an average price profile")]
    public static async Task<string> AddSetupLine(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID")] string profileId,
        [Description("Date of setup (format: yyyy-MM-dd)")] string date,
        [Description("Initial quantity")] decimal quantity,
        [Description("Initial cost basis")] decimal amount,
        [Description("Optional comment")] string? comment = null)
    {
        var result = await dispatcher.DispatchAsync(new AddLineCommand
        {
            ProfileId = profileId,
            Date = DateOnly.Parse(date),
            LineTypeId = 2, // Setup
            Quantity = quantity,
            Amount = amount,
            Comment = comment
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Setup line added with ID: {result.Value.LineId}";
    }

    /// <summary>
    /// Edits an existing line.
    /// </summary>
    [McpServerTool, Description("Edit an existing line in an average price profile")]
    public static async Task<string> EditLine(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID")] string profileId,
        [Description("The line ID to edit")] string lineId,
        [Description("Date (format: yyyy-MM-dd)")] string date,
        [Description("Line type: 0=Buy, 1=Sell, 2=Setup")] int lineTypeId,
        [Description("Quantity")] decimal quantity,
        [Description("Amount")] decimal amount,
        [Description("Optional comment")] string? comment = null)
    {
        var result = await dispatcher.DispatchAsync(new EditLineCommand
        {
            ProfileId = profileId,
            LineId = lineId,
            Date = DateOnly.Parse(date),
            LineTypeId = lineTypeId,
            Quantity = quantity,
            Amount = amount,
            Comment = comment
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Line {lineId} updated successfully";
    }

    /// <summary>
    /// Deletes a line from a profile.
    /// </summary>
    [McpServerTool, Description("Delete a line from an average price profile")]
    public static async Task<string> DeleteLine(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The profile ID")] string profileId,
        [Description("The line ID to delete")] string lineId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteLineCommand
        {
            ProfileId = profileId,
            LineId = lineId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Line {lineId} deleted successfully";
    }
}
