using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Accounts;
using Valt.Core.Modules.Budget.Categories;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Services;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;
using Valt.UI.Views.Main.Modals.ManageFixedExpenses;
using Valt.UI.Views.Main.Modals.TransactionEditor;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class FixedExpensesPanelViewModel : ValtViewModel, IDisposable
{
    private readonly IModalFactory _modalFactory;
    private readonly IFixedExpenseProvider _fixedExpenseProvider;
    private readonly IFixedExpenseRecordService _fixedExpenseRecordService;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;
    private readonly FilterState _filterState;
    private readonly IClock _clock;
    private readonly ILogger<FixedExpensesPanelViewModel> _logger;
    private readonly SecureModeState _secureModeState;

    [ObservableProperty] private AvaloniaList<FixedExpensesEntryViewModel> _fixedExpenseEntries = new();

    [ObservableProperty] private FixedExpensesEntryViewModel? _selectedFixedExpense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayRemainingFixedExpensesAmount))]
    private string _remainingFixedExpensesAmount = string.Empty;

    [ObservableProperty] private string? _remainingFixedExpensesTooltip;

    public FixedExpensesPanelViewModel(
        IModalFactory modalFactory,
        IFixedExpenseProvider fixedExpenseProvider,
        IFixedExpenseRecordService fixedExpenseRecordService,
        RatesState ratesState,
        CurrencySettings currencySettings,
        FilterState filterState,
        IClock clock,
        ILogger<FixedExpensesPanelViewModel> logger,
        SecureModeState secureModeState)
    {
        _modalFactory = modalFactory;
        _fixedExpenseProvider = fixedExpenseProvider;
        _fixedExpenseRecordService = fixedExpenseRecordService;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
        _filterState = filterState;
        _clock = clock;
        _logger = logger;
        _secureModeState = secureModeState;

        _filterState.PropertyChanged += FilterStateOnPropertyChanged;
        _secureModeState.PropertyChanged += SecureModeStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Register<TransactionListChanged>(this, OnTransactionListChangedReceive);
        WeakReferenceMessenger.Default.Register<FilterDateRangeChanged>(this, OnFilterDateRangeChangedReceive);

        _ = FetchFixedExpenses();
    }

    private void FilterStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(FixedExpenseCurrentMonthDescription));
    }

    private void SecureModeStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(DisplayRemainingFixedExpensesAmount));
            OnPropertyChanged(nameof(IsSecureModeEnabled));
        });
    }

    private void OnTransactionListChangedReceive(object recipient, TransactionListChanged message)
    {
        _ = FetchFixedExpenses();
    }

    private void OnFilterDateRangeChangedReceive(object recipient, FilterDateRangeChanged message)
    {
        _ = FetchFixedExpenses();
    }

    public string DisplayRemainingFixedExpensesAmount => _secureModeState.IsEnabled ? "---" : RemainingFixedExpensesAmount;

    public bool IsSecureModeEnabled => _secureModeState.IsEnabled;

    public string FixedExpenseCurrentMonthDescription =>
        $"({DateOnly.FromDateTime(_filterState.MainDate).ToString("MM/yy")})";

    private async Task FetchFixedExpenses()
    {
        try
        {
            var value = DateOnly.FromDateTime(_filterState.MainDate);
            var fixedExpenses = await _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(value);

            fixedExpenses = fixedExpenses.OrderBy(x => x.State).ThenBy(x => x.ReferenceDate).ToList();

            var minTotal = 0m;
            var maxTotal = 0m;

            var fixedExpenseHelper = new FixedExpenseHelper(_ratesState, _currencySettings);

            FixedExpenseEntries.Clear();
            foreach (var fixedExpense in fixedExpenses)
            {
                FixedExpenseEntries.Add(new FixedExpensesEntryViewModel(fixedExpense, _clock.GetCurrentLocalDate()));

                if (fixedExpense.State != FixedExpenseRecordState.Empty)
                    continue;

                var (fixedAmountMin, fixedAmountMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                    fixedExpense.FixedAmount,
                    fixedExpense.RangedAmountMin, fixedExpense.RangedAmountMax,
                    fixedExpense.Currency);

                minTotal += fixedAmountMin;
                maxTotal += fixedAmountMax;
            }

            if (minTotal == maxTotal)
            {
                RemainingFixedExpensesAmount =
                    $"{CurrencyDisplay.FormatFiat(minTotal, _currencySettings.MainFiatCurrency)}";
                RemainingFixedExpensesTooltip = null;
            }
            else
            {
                var middle = (maxTotal + minTotal) / 2;

                RemainingFixedExpensesAmount =
                    $"~ {CurrencyDisplay.FormatFiat(middle, _currencySettings.MainFiatCurrency)}";
                RemainingFixedExpensesTooltip =
                    $"{CurrencyDisplay.FormatFiat(minTotal, _currencySettings.MainFiatCurrency)} - {CurrencyDisplay.FormatFiat(maxTotal, _currencySettings.MainFiatCurrency)}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fixed expenses");
        }
    }

    partial void OnSelectedFixedExpenseChanged(FixedExpensesEntryViewModel? value)
    {
        WeakReferenceMessenger.Default.Send(new FixedExpenseChanged(value));
    }

    #region Commands

    [RelayCommand]
    private async Task ManageFixedExpenses()
    {
        var ownerWindow = GetUserControlOwnerWindow();

        var window = (ManageFixedExpensesView)await _modalFactory.CreateAsync(
            ApplicationModalNames.ManageFixedExpenses,
            ownerWindow, null)!;

        _ = await window.ShowDialog<ManageFixedExpensesViewModel.Response?>(ownerWindow!);

        await FetchFixedExpenses();
    }

    [RelayCommand]
    private async Task EditFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        var ownerWindow = GetUserControlOwnerWindow();

        var window = (FixedExpenseEditorView)await _modalFactory.CreateAsync(
            ApplicationModalNames.FixedExpenseEditor,
            ownerWindow, new FixedExpenseEditorViewModel.Request()
            {
                FixedExpenseId = entry.Id
            })!;

        _ = await window.ShowDialog<FixedExpenseEditorViewModel.Response?>(ownerWindow!);

        await FetchFixedExpenses();
    }

    [RelayCommand]
    private async Task OpenFixedExpense()
    {
        if (SelectedFixedExpense is null)
            return;

        TransactionEditorViewModel.Request request;
        if (SelectedFixedExpense.Paid)
        {
            request = new TransactionEditorViewModel.Request()
            {
                TransactionId = new TransactionId(SelectedFixedExpense.TransactionId!)
            };
        }
        else
        {
            request = new TransactionEditorViewModel.Request()
            {
                Date = DateTime.Now,
                AccountId = SelectedFixedExpense.DefaultAccountId is not null
                    ? new AccountId(SelectedFixedExpense.DefaultAccountId)
                    : null,
                CopyTransaction = false,
                DefaultFromFiatValue = SelectedFixedExpense.FixedAmount is not null
                    ? FiatValue.New(SelectedFixedExpense.FixedAmount)
                    : null,
                FixedExpenseReference =
                    new TransactionFixedExpenseReference(SelectedFixedExpense.Id, SelectedFixedExpense.ReferenceDate),
                Name = SelectedFixedExpense.Name,
                CategoryId = new CategoryId(SelectedFixedExpense!.CategoryId!)
            };
        }

        var ownerWindow = GetUserControlOwnerWindow()!;

        var modal =
            (TransactionEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.TransactionEditor,
                ownerWindow,
                request)!;

        var result = await modal.ShowDialog<TransactionEditorViewModel.Response?>(ownerWindow);

        if (result is null)
            return;

        WeakReferenceMessenger.Default.Send(new TransactionListChanged());
        await FetchFixedExpenses();
    }

    [RelayCommand(CanExecute = nameof(CanIgnoreFixedExpense))]
    public async Task IgnoreFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService.IgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanIgnoreFixedExpense(FixedExpensesEntryViewModel? entry) =>
        entry?.State == FixedExpenseRecordState.Empty;

    [RelayCommand(CanExecute = nameof(CanMarkFixedExpenseAsPaid))]
    public async Task MarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService.MarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanMarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.Empty or FixedExpenseRecordState.Ignored;

    [RelayCommand(CanExecute = nameof(CanUndoIgnoreFixedExpense))]
    public async Task UndoIgnoreFixedExpense(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService.UndoIgnoreFixedExpenseAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanUndoIgnoreFixedExpense(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.Ignored;

    [RelayCommand(CanExecute = nameof(CanUnmarkFixedExpenseAsPaid))]
    public async Task UnmarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry)
    {
        if (entry is null)
            return;

        await _fixedExpenseRecordService.UnmarkFixedExpenseAsPaidAsync(
            new TransactionFixedExpenseReference(entry.Id, entry.ReferenceDate));
        await FetchFixedExpenses();
    }

    public bool CanUnmarkFixedExpenseAsPaid(FixedExpensesEntryViewModel? entry) =>
        entry?.State is FixedExpenseRecordState.ManuallyPaid;

    #endregion

    public void Dispose()
    {
        _filterState.PropertyChanged -= FilterStateOnPropertyChanged;
        _secureModeState.PropertyChanged -= SecureModeStateOnPropertyChanged;

        WeakReferenceMessenger.Default.Unregister<TransactionListChanged>(this);
        WeakReferenceMessenger.Default.Unregister<FilterDateRangeChanged>(this);
    }
}
