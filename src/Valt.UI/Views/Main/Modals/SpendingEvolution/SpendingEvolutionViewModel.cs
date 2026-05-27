using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.Budget.Categories.DTOs;
using Valt.App.Modules.Budget.Categories.Queries.GetCategories;
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.UI.Base;
using Valt.UI.Views.Main.Modals.SpendingEvolution.Models;

namespace Valt.UI.Views.Main.Modals.SpendingEvolution;

public partial class SpendingEvolutionViewModel : ValtModalViewModel, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher;

    public SpendingEvolutionViewModel(IQueryDispatcher queryDispatcher)
    {
        _queryDispatcher = queryDispatcher;
        ChartData = new SpendingEvolutionChartData();
    }

    [ObservableProperty]
    private SpendingEvolutionChartData _chartData;

    [ObservableProperty]
    private ObservableCollection<CategorySelectionItem> _categoryItems = new();

    [ObservableProperty]
    private int _selectedTimeRangeMonths = 24; // Default: 24 months

    [ObservableProperty]
    private string? _preSelectedCategoryId;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SpendingEvolutionDataDto? _currentData;

    public int[] TimeRangeOptions { get; } = new[] { 12, 24, 36, 48, 60 };

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is string categoryId)
        {
            PreSelectedCategoryId = categoryId;
        }

        await LoadCategoriesAsync();
        await LoadDataAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());

        // Build tree structure
        var allItems = categories.Items.Select(c => new CategorySelectionItem(
            c.Id, null, c.Name, c.Unicode, c.Color)).ToList();

        // Group by parent-child (existing pattern from ManageCategories)
        // For now, flat list is acceptable — tree structure added in Plan 03
        CategoryItems.Clear();
        foreach (var item in allItems)
        {
            CategoryItems.Add(item);
        }

        // If pre-selected category exists, select only that one
        if (!string.IsNullOrEmpty(PreSelectedCategoryId))
        {
            foreach (var item in CategoryItems)
            {
                item.IsSelected = item.Id == PreSelectedCategoryId;
            }
        }
        else
        {
            // Select all by default
            foreach (var item in CategoryItems)
            {
                item.IsSelected = true;
            }
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var fromDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-SelectedTimeRangeMonths));
            var toDate = DateOnly.FromDateTime(DateTime.Now);

            var selectedCategoryIds = CategoryItems
                .Where(c => c.IsSelected)
                .Select(c => c.Id)
                .ToArray();

            var query = new GetSpendingEvolutionQuery
            {
                From = fromDate,
                To = toDate,
                CategoryIds = selectedCategoryIds,
                ShowHiddenAccounts = false
            };

            CurrentData = await _queryDispatcher.DispatchAsync(query);
            ChartData.RefreshChart(CurrentData);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedTimeRangeMonthsChanged(int value)
    {
        _ = LoadDataAsync();
    }

    // Hook up to CategoryItems changes — in Plan 03 we'll add proper event handling
    // For now, add a command that can be called when selection changes
    [RelayCommand]
    private async Task OnCategorySelectionChanged()
    {
        await LoadDataAsync();
    }

    public void Dispose()
    {
        ChartData?.Dispose();
    }
}
