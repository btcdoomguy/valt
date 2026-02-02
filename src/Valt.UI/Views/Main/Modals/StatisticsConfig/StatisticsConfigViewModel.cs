using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.StatisticsConfig;

public partial class StatisticsConfigViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly IConfigurationManager _configurationManager = null!;

    /// <summary>
    /// All available categories
    /// </summary>
    public AvaloniaList<CategorySelectItem> AllCategories { get; } = new();

    /// <summary>
    /// Categories selected to be EXCLUDED from statistics
    /// </summary>
    public AvaloniaList<CategorySelectItem> ExcludedCategories { get; } = new();

    public StatisticsConfigViewModel()
    {
        // Design-time constructor
    }

    public StatisticsConfigViewModel(
        IQueryDispatcher queryDispatcher,
        IConfigurationManager configurationManager)
    {
        _queryDispatcher = queryDispatcher;
        _configurationManager = configurationManager;
    }

    public override async Task OnBindParameterAsync()
    {
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        AllCategories.Clear();
        ExcludedCategories.Clear();

        // Load all categories using query dispatcher
        var result = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());
        var excludedIds = _configurationManager.GetStatisticsExcludedCategoryIds().ToHashSet();

        var categoryItems = new List<CategorySelectItem>();
        foreach (var category in result.Items)
        {
            // CategoryDTO.Name already contains the full path (e.g., "Parent >> Child")
            categoryItems.Add(new CategorySelectItem(category.Id, category.Name));
        }

        // Sort by name
        var sortedItems = categoryItems.OrderBy(x => x.Name).ToList();

        AllCategories.AddRange(sortedItems);

        // Select currently excluded categories
        foreach (var item in AllCategories.Where(c => excludedIds.Contains(c.Id)))
        {
            ExcludedCategories.Add(item);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // Get IDs of excluded categories
        var excludedIds = ExcludedCategories.Select(c => c.Id);

        // Save to configuration manager
        _configurationManager.SetStatisticsExcludedCategoryIds(excludedIds);

        CloseDialog?.Invoke(new Response(true));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    public record Response(bool Ok);
}

public record CategorySelectItem(string Id, string Name);
