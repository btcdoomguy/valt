using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
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
    private bool _isDataLoading;
    private CancellationTokenSource? _categoryChangeCts;

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

    [ObservableProperty]
    private decimal _fiatIncreasePercent;

    [ObservableProperty]
    private decimal _btcIncreasePercent;

    [ObservableProperty]
    private string _fiatIncreasePercentText = "N/A";

    [ObservableProperty]
    private string _btcIncreasePercentText = "N/A";

    [ObservableProperty]
    private bool _hasMissingPriceInSats;

    [ObservableProperty]
    private SolidColorBrush _fiatIncreaseBrush = new(Colors.Gray);

    [ObservableProperty]
    private SolidColorBrush _btcIncreaseBrush = new(Colors.Gray);

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

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _queryDispatcher.DispatchAsync(new GetCategoriesQuery());

        // Unsubscribe from old items
        foreach (var item in GetAllItems(CategoryItems))
        {
            item.PropertyChanged -= OnCategoryItemPropertyChanged;
        }

        CategoryItems.Clear();

        // Build tree structure
        var allItems = categories.Items
            .Select(c => new CategorySelectionItem(c.Id, c.ParentId, c.Name, c.Unicode, c.Color))
            .ToList();

        var itemLookup = allItems.ToDictionary(i => i.Id);

        // Wire up parent-child relationships
        foreach (var item in allItems)
        {
            if (!string.IsNullOrEmpty(item.ParentId) && itemLookup.TryGetValue(item.ParentId, out var parent))
            {
                parent.SubNodes.Add(item);
            }
        }

        // Add only root items to the top-level collection
        var rootItems = allItems.Where(i => string.IsNullOrEmpty(i.ParentId) || !itemLookup.ContainsKey(i.ParentId)).ToList();
        foreach (var root in rootItems)
        {
            CategoryItems.Add(root);
        }

        // Subscribe to all items' PropertyChanged for IsSelected changes
        foreach (var item in GetAllItems(CategoryItems))
        {
            item.PropertyChanged += OnCategoryItemPropertyChanged;
        }

        ApplyPreSelection();
    }

    private static IEnumerable<CategorySelectionItem> GetAllItems(IEnumerable<CategorySelectionItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var child in GetAllItems(item.SubNodes))
            {
                yield return child;
            }
        }
    }

    private void OnCategoryItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CategorySelectionItem.IsSelected))
        {
            // Debounce: cancel pending reload and schedule a new one
            _categoryChangeCts?.Cancel();
            _categoryChangeCts = new CancellationTokenSource();
            var token = _categoryChangeCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150, token);
                    if (!token.IsCancellationRequested)
                    {
                        await LoadDataAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when debounce cancels
                }
            });
        }
    }

    private void ApplyPreSelection()
    {
        if (!string.IsNullOrEmpty(PreSelectedCategoryId))
        {
            // Right-click mode: select only the pre-selected category
            foreach (var item in GetAllItems(CategoryItems))
            {
                item.IsSelected = item.Id == PreSelectedCategoryId;
            }
        }
        else
        {
            // Menu mode: select all categories
            foreach (var item in GetAllItems(CategoryItems))
            {
                item.IsSelected = true;
            }
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_isDataLoading) return;
        _isDataLoading = true;
        IsLoading = true;
        try
        {
            var fromDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-SelectedTimeRangeMonths));
            var toDate = DateOnly.FromDateTime(DateTime.Now);

            var selectedCategoryIds = GetAllItems(CategoryItems)
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

            // Update indicators and warnings
            HasMissingPriceInSats = CurrentData.HasMissingPriceInSats;
            CalculateIndicators();
        }
        finally
        {
            IsLoading = false;
            _isDataLoading = false;
        }
    }

    private void CalculateIndicators()
    {
        if (CurrentData?.Months == null || CurrentData.Months.Count < 2)
        {
            FiatIncreasePercent = 0;
            BtcIncreasePercent = 0;
            FiatIncreasePercentText = "N/A";
            BtcIncreasePercentText = "N/A";
            FiatIncreaseBrush = new SolidColorBrush(Colors.Gray);
            BtcIncreaseBrush = new SolidColorBrush(Colors.Gray);
            return;
        }

        var firstMonth = CurrentData.Months.First();
        var lastMonth = CurrentData.Months.Last();

        // Fiat increase
        if (firstMonth.FiatTotal > 0)
        {
            FiatIncreasePercent = ((lastMonth.FiatTotal - firstMonth.FiatTotal) / firstMonth.FiatTotal) * 100;
            FiatIncreasePercentText = $"{(FiatIncreasePercent >= 0 ? "+" : "")}{FiatIncreasePercent:F1}%";
            // Green for decrease (good), Red for increase (bad)
            FiatIncreaseBrush = new SolidColorBrush(
                FiatIncreasePercent <= 0 ? Colors.Green : Colors.Red);
        }
        else
        {
            FiatIncreasePercent = 0;
            FiatIncreasePercentText = "N/A";
            FiatIncreaseBrush = new SolidColorBrush(Colors.Gray);
        }

        // BTC increase
        if (firstMonth.SatsTotal > 0)
        {
            BtcIncreasePercent = ((lastMonth.SatsTotal - firstMonth.SatsTotal) / (decimal)firstMonth.SatsTotal) * 100;
            BtcIncreasePercentText = $"{(BtcIncreasePercent >= 0 ? "+" : "")}{BtcIncreasePercent:F1}%";
            // Green for decrease (good), Red for increase (bad)
            BtcIncreaseBrush = new SolidColorBrush(
                BtcIncreasePercent <= 0 ? Colors.Green : Colors.Red);
        }
        else
        {
            BtcIncreasePercent = 0;
            BtcIncreasePercentText = "N/A";
            BtcIncreaseBrush = new SolidColorBrush(Colors.Gray);
        }
    }

    partial void OnSelectedTimeRangeMonthsChanged(int value)
    {
        _ = LoadDataAsync();
    }

    public void Dispose()
    {
        _categoryChangeCts?.Cancel();
        _categoryChangeCts?.Dispose();

        foreach (var item in GetAllItems(CategoryItems))
        {
            item.PropertyChanged -= OnCategoryItemPropertyChanged;
        }

        ChartData?.Dispose();
    }
}
