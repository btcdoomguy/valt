using CommunityToolkit.Mvvm.ComponentModel;
using Valt.App.Modules.Budget.Categories.DTOs;

namespace Valt.UI.Views.Main.Modals.ImportWizard.Models;

/// <summary>
/// Represents a mapping between a CSV category name and an existing Valt category.
/// </summary>
public partial class CategoryMappingItem : ObservableObject
{
    /// <summary>
    /// The category name as it appears in the CSV file.
    /// </summary>
    [ObservableProperty]
    private string _csvCategoryName = string.Empty;

    /// <summary>
    /// The matching existing category, if found.
    /// </summary>
    [ObservableProperty]
    private CategoryDTO? _existingCategory;

    /// <summary>
    /// Whether this category will be created as new.
    /// </summary>
    [ObservableProperty]
    private bool _isNew;

    /// <summary>
    /// Creates a CategoryMappingItem from a CSV category name.
    /// </summary>
    public static CategoryMappingItem Create(string csvCategoryName, CategoryDTO? existingCategory)
    {
        return new CategoryMappingItem
        {
            CsvCategoryName = csvCategoryName,
            ExistingCategory = existingCategory,
            IsNew = existingCategory is null
        };
    }

    /// <summary>
    /// Gets a display string for the status.
    /// </summary>
    public string StatusDisplay => IsNew ? "New" : "Existing";
}
