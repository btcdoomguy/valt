using System.ComponentModel;
using ModelContextProtocol.Server;
using Valt.App.Kernel.Commands;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.Commands.CreateCategory;
using Valt.App.Modules.Budget.Categories.Commands.DeleteCategory;
using Valt.App.Modules.Budget.Categories.Commands.EditCategory;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.Infra.Kernel.Notifications;
using Valt.Infra.Mcp.Notifications;

namespace Valt.Infra.Mcp.Tools.Budget;

/// <summary>
/// MCP tools for category management.
/// </summary>
[McpServerToolType]
public class CategoryTools
{
    /// <summary>
    /// Gets all categories in a hierarchical structure.
    /// </summary>
    [McpServerTool, Description("Get all categories with their hierarchy")]
    public static async Task<IReadOnlyList<CategoryDTO>> GetCategories(
        IQueryDispatcher dispatcher)
    {
        var result = await dispatcher.DispatchAsync(new GetCategoriesQuery());
        return result.Items;
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    [McpServerTool, Description("Create a new category")]
    public static async Task<string> CreateCategory(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("Category name")] string name,
        [Description("Icon ID for the category")] string iconId,
        [Description("Optional parent category ID to create a subcategory")] string? parentId = null)
    {
        var result = await dispatcher.DispatchAsync(new CreateCategoryCommand
        {
            Name = name,
            IconId = iconId,
            ParentId = parentId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Category created with ID: {result.Value.CategoryId}";
    }

    /// <summary>
    /// Edits an existing category.
    /// </summary>
    [McpServerTool, Description("Edit an existing category (all fields are required)")]
    public static async Task<string> EditCategory(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The category ID to edit")] string categoryId,
        [Description("Category name")] string name,
        [Description("Icon ID")] string iconId)
    {
        var result = await dispatcher.DispatchAsync(new EditCategoryCommand
        {
            CategoryId = categoryId,
            Name = name,
            IconId = iconId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Category {categoryId} updated successfully";
    }

    /// <summary>
    /// Deletes a category.
    /// </summary>
    [McpServerTool, Description("Delete a category (will fail if it has transactions)")]
    public static async Task<string> DeleteCategory(
        ICommandDispatcher dispatcher,
        INotificationPublisher publisher,
        [Description("The category ID to delete")] string categoryId)
    {
        var result = await dispatcher.DispatchAsync(new DeleteCategoryCommand
        {
            CategoryId = categoryId
        });

        if (result.IsFailure)
        {
            return $"Error: {result.Error?.Message ?? "Unknown error"}";
        }

        await publisher.PublishAsync(new McpDataChangedNotification());
        return $"Category {categoryId} deleted successfully";
    }
}
