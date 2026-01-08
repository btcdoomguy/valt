using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Core.Modules.Budget.FixedExpenses.Contracts;
using Valt.Core.Modules.Budget.Transactions.Contracts;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries;
using Valt.Infra.Modules.Budget.FixedExpenses.Queries.DTOs;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Base.Utils;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.Services;
using Valt.UI.Services.MessageBoxes;
using Valt.UI.State;
using Valt.UI.Views.Main.Modals.FixedExpenseEditor;
using Valt.UI.Views.Main.Modals.FixedExpenseHistory;

namespace Valt.UI.Views.Main.Modals.ManageFixedExpenses;

public partial class ManageFixedExpensesViewModel : ValtModalViewModel
{
    private readonly IFixedExpenseQueries _fixedExpenseQueries;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IFixedExpenseRepository _fixedExpenseRepository;
    private readonly IModalFactory _modalFactory;
    private readonly RatesState _ratesState;
    private readonly CurrencySettings _currencySettings;

    public AvaloniaList<FixedExpenseListItemViewModel> FixedExpenses { get; set; } = new();
    [ObservableProperty] private FixedExpenseListItemViewModel? _selectedFixedExpense;

    [ObservableProperty] private string _monthlyExpenses;
    [ObservableProperty] private string _yearlyExpenses;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public ManageFixedExpensesViewModel()
    {
        if (!Design.IsDesignMode) return;

        FixedExpenses.Add(new FixedExpenseListItemViewModel()
        {
            Id = "1",
            Name = "Rent",
            Category = "House",
            CategoryIcon = Icon.Empty,
            DefaultAccount = "Main Account 1",
            DefaultAccountIcon = Icon.Empty,
            Amount = "$ 1000.00",
            Period = "Monthly",
            Day = "5",
            Enabled = true
        });
    }

    public ManageFixedExpensesViewModel(IFixedExpenseQueries fixedExpenseQueries,
        ITransactionRepository transactionRepository,
        IFixedExpenseRepository fixedExpenseRepository,
        IModalFactory modalFactory,
        RatesState ratesState,
        CurrencySettings currencySettings)
    {
        _fixedExpenseQueries = fixedExpenseQueries;
        _transactionRepository = transactionRepository;
        _fixedExpenseRepository = fixedExpenseRepository;
        _modalFactory = modalFactory;
        _ratesState = ratesState;
        _currencySettings = currencySettings;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await FetchFixedExpensesAsync();
    }

    private async Task FetchFixedExpensesAsync()
    {
        var fixedExpensesQueryResult = (await _fixedExpenseQueries.GetFixedExpensesAsync())
            .OrderByDescending(x => x.Enabled)
            .ThenBy(x => x.LatestRange.Day)
            .ToList();

        FixedExpenses.Clear();
        foreach (var fixedExpenseDTO in fixedExpensesQueryResult)
            FixedExpenses.Add(new FixedExpenseListItemViewModel()
            {
                Id = fixedExpenseDTO.Id,
                Name = fixedExpenseDTO.Name,
                Category = fixedExpenseDTO.CategoryName ?? string.Empty,
                CategoryIcon = fixedExpenseDTO.CategoryIcon is not null
                    ? Icon.RestoreFromId(fixedExpenseDTO.CategoryIcon)
                    : Icon.Empty,
                DefaultAccount = fixedExpenseDTO.DefaultAccountName ?? string.Empty,
                DefaultAccountIcon = fixedExpenseDTO.DefaultAccountIcon is not null
                    ? Icon.RestoreFromId(fixedExpenseDTO.DefaultAccountIcon)
                    : Icon.Empty,
                Currency = fixedExpenseDTO.Currency ?? string.Empty,
                Amount = fixedExpenseDTO.LatestRange.FixedAmount is not null
                    ? fixedExpenseDTO.LatestRange.FixedAmountFormatted!
                    : $"{fixedExpenseDTO.LatestRange.RangedAmountMinFormatted} - {fixedExpenseDTO.LatestRange.RangedAmountMaxFormatted}",
                Period = GetPeriodDescription(fixedExpenseDTO.LatestRange.PeriodId),
                Day = ((FixedExpensePeriods)fixedExpenseDTO.LatestRange.PeriodId).IsPeriodUsingFixedDay()
                    ? fixedExpenseDTO.LatestRange.Day.ToString()
                    : ((DayOfWeek)fixedExpenseDTO.LatestRange.Day).ToValtCaption(),
                Enabled = fixedExpenseDTO.Enabled
            });

        await RefreshTotalsAsync(fixedExpensesQueryResult);
    }

    private string GetPeriodDescription(int periodId)
    {
        var period = (FixedExpensePeriods)periodId;

        return period switch
        {
            FixedExpensePeriods.Monthly => language.ManageFixedExpenses_PeriodRange_Monthly,
            FixedExpensePeriods.Yearly => language.ManageFixedExpenses_PeriodRange_Yearly,
            FixedExpensePeriods.Weekly => language.ManageFixedExpenses_PeriodRange_Weekly,
            FixedExpensePeriods.Biweekly => language.ManageFixedExpenses_PeriodRange_Biweekly,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task RefreshTotalsAsync(IReadOnlyList<FixedExpenseDto> fixedExpensesQueryResult)
    {
        if (_ratesState.FiatRates is null)
            return;

        var minTotal = 0m;
        var maxTotal = 0m;

        var fixedExpenseHelper = new FixedExpenseHelper(_ratesState, _currencySettings);

        foreach (var fixedExpenseDTO in fixedExpensesQueryResult)
        {
            var (fixedAmountMin, fixedAmountMax) = fixedExpenseHelper.CalculateFixedExpenseRange(fixedExpenseDTO.LatestRange.FixedAmount,
                fixedExpenseDTO.LatestRange.RangedAmountMin, fixedExpenseDTO.LatestRange.RangedAmountMax,
                fixedExpenseDTO.DisplayCurrency);

            minTotal += fixedExpenseDTO.LatestRange.PeriodId == (int)FixedExpensePeriods.Monthly
                ? fixedAmountMin * 12
                : fixedAmountMin;
            maxTotal += fixedExpenseDTO.LatestRange.PeriodId == (int)FixedExpensePeriods.Monthly
                ? fixedAmountMax * 12
                : fixedAmountMax;
        }

        MonthlyExpenses =
            $"{CurrencyDisplay.FormatFiat(minTotal / 12, _currencySettings.MainFiatCurrency)} - {CurrencyDisplay.FormatFiat(maxTotal / 12, _currencySettings.MainFiatCurrency)}";
        YearlyExpenses =
            $"{CurrencyDisplay.FormatFiat(minTotal, _currencySettings.MainFiatCurrency)} - {CurrencyDisplay.FormatFiat(maxTotal, _currencySettings.MainFiatCurrency)}";
    }

    [RelayCommand]
    private async Task AddFixedExpense()
    {
        var window = (FixedExpenseEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.FixedExpenseEditor,
            OwnerWindow, null)!;

        _ = await window.ShowDialog<FixedExpenseEditorViewModel.Response?>(OwnerWindow!);

        await FetchFixedExpensesAsync();
    }

    [RelayCommand]
    private async Task EditFixedExpense()
    {
        if (SelectedFixedExpense is null)
            return;

        var window = (FixedExpenseEditorView)await _modalFactory.CreateAsync(ApplicationModalNames.FixedExpenseEditor,
            OwnerWindow, new FixedExpenseEditorViewModel.Request()
            {
                FixedExpenseId = SelectedFixedExpense?.Id
            })!;

        _ = await window.ShowDialog<FixedExpenseEditorViewModel.Response?>(OwnerWindow!);

        await FetchFixedExpensesAsync();
    }

    [RelayCommand]
    private async Task ViewHistory()
    {
        if (SelectedFixedExpense is null)
            return;

        var window = (FixedExpenseHistoryView)await _modalFactory.CreateAsync(ApplicationModalNames.FixedExpenseHistory,
            OwnerWindow, new FixedExpenseHistoryViewModel.Request
            {
                FixedExpenseId = SelectedFixedExpense.Id
            })!;

        _ = await window.ShowDialog<object?>(OwnerWindow!);
    }

    [RelayCommand]
    private async Task DeleteFixedExpense()
    {
        if (SelectedFixedExpense is null)
            return;

        var response = await MessageBoxHelper.ShowQuestionAsync(language.ManageFixedExpenses_Delete_Alert,
            language.ManageFixedExpenses_Delete_Message, OwnerWindow!);

        if (!response)
            return;

        var fixedExpense = await _fixedExpenseRepository.GetFixedExpenseByIdAsync(SelectedFixedExpense!.Id);

        if (fixedExpense is null)
            return;

        await _fixedExpenseRepository.DeleteFixedExpenseAsync(fixedExpense.Id);

        await FetchFixedExpensesAsync();
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record FixedExpenseListItemViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Icon CategoryIcon { get; set; }
        public string DefaultAccount { get; set; }
        public Icon DefaultAccountIcon { get; set; }
        public string Currency { get; set; }
        public string Amount { get; set; }
        public string Period { get; set; }
        public string Day { get; set; }
        public bool Enabled { get; set; }

        public string EnabledDescription => Enabled ? language.Yes : language.No;
    }

    public record Response(bool Ok);
}