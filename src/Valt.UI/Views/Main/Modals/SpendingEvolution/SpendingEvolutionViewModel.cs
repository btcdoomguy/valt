using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using Valt.App.Kernel.Queries;
using Valt.App.Modules.SpendingEvolution.DTOs;
using Valt.App.Modules.SpendingEvolution.Queries;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Infra.DataAccess;
using Valt.Infra.Modules.Configuration;
using Valt.UI.Base;
using Valt.UI.Lang;
using Valt.UI.Services.MessageBoxes;

namespace Valt.UI.Views.Main.Modals.SpendingEvolution;

public record TimeRangeOption(int Months, string DisplayText);
public record SelectItem(string Id, string Name);

public partial class SpendingEvolutionViewModel : ValtModalViewModel, IDisposable
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILocalDatabase _localDatabase;
    private readonly IConfigurationManager _configurationManager;
    private readonly IClock _clock;
    private bool _isDataLoading;
    private CancellationTokenSource? _filterChangeCts;

    public SpendingEvolutionViewModel()
    {
        
    }

    public SpendingEvolutionViewModel(
        IQueryDispatcher queryDispatcher,
        ILocalDatabase localDatabase,
        IConfigurationManager configurationManager,
        IClock clock)
    {
        _queryDispatcher = queryDispatcher;
        _localDatabase = localDatabase;
        _configurationManager = configurationManager;
        _clock = clock;
        ChartData = new SpendingEvolutionChartData();
        SelectedTimeRangeOption = TimeRangeOptions.FirstOrDefault(x => x.Months == 24);
    }

    [ObservableProperty]
    private SpendingEvolutionChartData _chartData;

    [ObservableProperty]
    private AvaloniaList<SelectItem> _availableAccounts = new();

    [ObservableProperty]
    private AvaloniaList<SelectItem> _selectedAccounts = new();

    [ObservableProperty]
    private AvaloniaList<SelectItem> _availableCategories = new();

    [ObservableProperty]
    private AvaloniaList<SelectItem> _selectedCategories = new();

    [ObservableProperty]
    private TimeRangeOption? _selectedTimeRangeOption;

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
    private string _fiatIncreasePercentText = language.SpendingEvolution_NA;

    [ObservableProperty]
    private string _btcIncreasePercentText = language.SpendingEvolution_NA;

    [ObservableProperty]
    private bool _hasMissingPriceInSats;

    [ObservableProperty]
    private SolidColorBrush _fiatIncreaseBrush = new(Colors.Gray);

    [ObservableProperty]
    private SolidColorBrush _btcIncreaseBrush = new(Colors.Gray);

    public TimeRangeOption[] TimeRangeOptions { get; } = new[]
    {
        new TimeRangeOption(12, string.Format(language.SpendingEvolution_Months, 12)),
        new TimeRangeOption(24, string.Format(language.SpendingEvolution_Months, 24)),
        new TimeRangeOption(36, string.Format(language.SpendingEvolution_Months, 36)),
        new TimeRangeOption(48, string.Format(language.SpendingEvolution_Months, 48)),
        new TimeRangeOption(60, string.Format(language.SpendingEvolution_Months, 60)),
    };

    public override async Task OnBindParameterAsync()
    {
        if (Parameter is string categoryId)
        {
            PreSelectedCategoryId = categoryId;
        }

        PrepareAccountsAndCategoriesList();

        SelectedAccounts.CollectionChanged += OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged += OnSelectedFiltersChanged;

        await LoadDataAsync();
    }

    [RelayCommand]
    private void Close()
    {
        CloseWindow?.Invoke();
    }

    private void PrepareAccountsAndCategoriesList()
    {
        AvailableAccounts.Clear();
        SelectedAccounts.Clear();
        AvailableCategories.Clear();
        SelectedCategories.Clear();

        // Load accounts
        var accounts = _localDatabase.GetAccounts().FindAll()
            .OrderByDescending(x => x.Visible)
            .ThenBy(x => x.DisplayOrder)
            .Select(x => new SelectItem(x.Id.ToString(), x.Name))
            .ToList();

        AvailableAccounts.AddRange(accounts);
        SelectedAccounts.AddRange(AvailableAccounts);

        // Load categories with parent prefix
        var categories = _localDatabase.GetCategories().FindAll().ToDictionary(x => x.Id.ToString());

        var parsedCategories = new List<SelectItem>();
        foreach (var category in categories)
        {
            var name = category.Value.Name;
            if (category.Value.ParentId is not null)
                name = categories[category.Value.ParentId.ToString()].Name + " >> " + name;

            parsedCategories.Add(new SelectItem(category.Key, name));
        }

        AvailableCategories.AddRange(parsedCategories.OrderBy(x => x.Name));
        SelectedCategories.AddRange(AvailableCategories);

        // Apply saved filter defaults
        ApplySavedFilterDefaults();

        // Apply pre-selection if opened from context menu
        if (!string.IsNullOrEmpty(PreSelectedCategoryId))
        {
            var preSelected = AvailableCategories.FirstOrDefault(c => c.Id == PreSelectedCategoryId);
            if (preSelected is not null)
            {
                SelectedCategories.Clear();
                SelectedCategories.Add(preSelected);
            }
        }
    }

    private void ApplySavedFilterDefaults()
    {
        var excludedCategoryIds = _configurationManager.GetSpendingEvolutionCategoryFilterExcludedIds().ToHashSet();
        if (excludedCategoryIds.Count > 0)
        {
            var toRemove = SelectedCategories.Where(c => excludedCategoryIds.Contains(c.Id)).ToList();
            foreach (var item in toRemove)
                SelectedCategories.Remove(item);
        }

        var excludedAccountIds = _configurationManager.GetSpendingEvolutionAccountFilterExcludedIds().ToHashSet();
        if (excludedAccountIds.Count > 0)
        {
            var toRemove = SelectedAccounts.Where(a => excludedAccountIds.Contains(a.Id)).ToList();
            foreach (var item in toRemove)
                SelectedAccounts.Remove(item);
        }
    }

    private void OnSelectedFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Debounce: cancel pending reload and schedule a new one
        _filterChangeCts?.Cancel();
        _filterChangeCts = new CancellationTokenSource();
        var token = _filterChangeCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!token.IsCancellationRequested)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () => await LoadDataAsync());
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when debounce cancels
            }
        });
    }

    [RelayCommand]
    private async Task SaveFilter()
    {
        var ownerWindow = GetUserControlOwnerWindow?.Invoke();
        if (ownerWindow is null) return;

        var confirmed = await MessageBoxHelper.ShowQuestionAsync(
            language.SpendingEvolution_SaveConfirmTitle,
            language.SpendingEvolution_SaveConfirmMessage,
            ownerWindow);

        if (!confirmed) return;

        var selectedCategoryIds = SelectedCategories.Select(c => c.Id).ToHashSet();
        var excludedCategoryIds = AvailableCategories.Where(c => !selectedCategoryIds.Contains(c.Id)).Select(c => c.Id);
        _configurationManager.SetSpendingEvolutionCategoryFilterExcludedIds(excludedCategoryIds);

        var selectedAccountIds = SelectedAccounts.Select(a => a.Id).ToHashSet();
        var excludedAccountIds = AvailableAccounts.Where(a => !selectedAccountIds.Contains(a.Id)).Select(a => a.Id);
        _configurationManager.SetSpendingEvolutionAccountFilterExcludedIds(excludedAccountIds);

        await MessageBoxHelper.ShowAlertAsync(
            language.SpendingEvolution_SaveConfirmTitle,
            language.SpendingEvolution_SaveSuccess,
            ownerWindow);
    }

    [RelayCommand]
    private async Task LoadFilter()
    {
        // Temporarily detach event handlers to avoid triggering multiple LoadDataAsync calls
        SelectedAccounts.CollectionChanged -= OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged -= OnSelectedFiltersChanged;

        try
        {
            var excludedCategoryIds = _configurationManager.GetSpendingEvolutionCategoryFilterExcludedIds().ToHashSet();
            if (excludedCategoryIds.Count == 0)
            {
                SelectedCategories.Clear();
                SelectedCategories.AddRange(AvailableCategories);
            }
            else
            {
                var newSelection = AvailableCategories.Where(c => !excludedCategoryIds.Contains(c.Id)).ToList();
                SelectedCategories.Clear();
                SelectedCategories.AddRange(newSelection);
            }

            var excludedAccountIds = _configurationManager.GetSpendingEvolutionAccountFilterExcludedIds().ToHashSet();
            if (excludedAccountIds.Count == 0)
            {
                SelectedAccounts.Clear();
                SelectedAccounts.AddRange(AvailableAccounts);
            }
            else
            {
                var newSelection = AvailableAccounts.Where(a => !excludedAccountIds.Contains(a.Id)).ToList();
                SelectedAccounts.Clear();
                SelectedAccounts.AddRange(newSelection);
            }
        }
        finally
        {
            SelectedAccounts.CollectionChanged += OnSelectedFiltersChanged;
            SelectedCategories.CollectionChanged += OnSelectedFiltersChanged;
        }

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_isDataLoading) return;
        _isDataLoading = true;
        IsLoading = true;
        try
        {
            var months = SelectedTimeRangeOption?.Months ?? 24;
            var today = _clock.GetCurrentLocalDate();
            var fromDate = DateOnly.FromDateTime(today.ToDateTime(TimeOnly.MinValue).AddMonths(-months));
            var toDate = today;

            var selectedCategoryIds = SelectedCategories.Select(c => c.Id).ToArray();
            var selectedAccountIds = SelectedAccounts.Select(a => a.Id).ToArray();

            var query = new GetSpendingEvolutionQuery
            {
                From = fromDate,
                To = toDate,
                CategoryIds = selectedCategoryIds,
                AccountIds = selectedAccountIds
            };

            CurrentData = await _queryDispatcher.DispatchAsync(query);
            ChartData.RefreshChart(CurrentData);

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
            FiatIncreasePercentText = language.SpendingEvolution_NA;
            BtcIncreasePercentText = language.SpendingEvolution_NA;
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
            FiatIncreaseBrush = new SolidColorBrush(
                FiatIncreasePercent <= 0 ? Colors.Green : Colors.Red);
        }
        else
        {
            FiatIncreasePercent = 0;
            FiatIncreasePercentText = language.SpendingEvolution_NA;
            FiatIncreaseBrush = new SolidColorBrush(Colors.Gray);
        }

        // BTC increase
        if (firstMonth.SatsTotal > 0)
        {
            BtcIncreasePercent = ((lastMonth.SatsTotal - firstMonth.SatsTotal) / (decimal)firstMonth.SatsTotal) * 100;
            BtcIncreasePercentText = $"{(BtcIncreasePercent >= 0 ? "+" : "")}{BtcIncreasePercent:F1}%";
            BtcIncreaseBrush = new SolidColorBrush(
                BtcIncreasePercent <= 0 ? Colors.Green : Colors.Red);
        }
        else
        {
            BtcIncreasePercent = 0;
            BtcIncreasePercentText = language.SpendingEvolution_NA;
            BtcIncreaseBrush = new SolidColorBrush(Colors.Gray);
        }
    }

    partial void OnSelectedTimeRangeOptionChanged(TimeRangeOption? value)
    {
        if (value != null)
        {
            _ = LoadDataAsync();
        }
    }

    public void Dispose()
    {
        _filterChangeCts?.Cancel();
        _filterChangeCts?.Dispose();

        SelectedAccounts.CollectionChanged -= OnSelectedFiltersChanged;
        SelectedCategories.CollectionChanged -= OnSelectedFiltersChanged;

        ChartData?.Dispose();
    }
}
