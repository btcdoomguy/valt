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
using Valt.UI.Views.Main.Modals.StatisticsConfig;

namespace Valt.UI.Views.Main.Modals.ReportsCategoryFilterConfig;

public partial class ReportsCategoryFilterConfigViewModel : ValtModalViewModel
{
    private readonly IQueryDispatcher _queryDispatcher = null!;
    private readonly IConfigurationManager _configurationManager = null!;

    /// <summary>
    /// All available categories
    /// </summary>
    public AvaloniaList<CategorySelectItem> AllCategories { get; } = new();

    /// <summary>
    /// Categories selected to be EXCLUDED from reports analytics
    /// </summary>
    public AvaloniaList<CategorySelectItem> ExcludedCategories { get; } = new();

    public ReportsCategoryFilterConfigViewModel()
    {
        // Design-time constructor
    }

    public ReportsCategoryFilterConfigViewModel(
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

        var result = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());
        var excludedIds = _configurationManager.GetReportsAnalyticsCategoryFilterExcludedIds().ToHashSet();

        var categoryItems = new List<CategorySelectItem>();
        foreach (var category in result.Items)
        {
            categoryItems.Add(new CategorySelectItem(category.Id, category.Name));
        }

        var sortedItems = categoryItems.OrderBy(x => x.Name).ToList();

        AllCategories.AddRange(sortedItems);

        foreach (var item in AllCategories.Where(c => excludedIds.Contains(c.Id)))
        {
            ExcludedCategories.Add(item);
        }
    }

    [RelayCommand]
    private void Save()
    {
        var excludedIds = ExcludedCategories.Select(c => c.Id);
        _configurationManager.SetReportsAnalyticsCategoryFilterExcludedIds(excludedIds);

        CloseDialog?.Invoke(new Response(true));
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow?.Invoke();
    }

    public record Response(bool Ok);
}
