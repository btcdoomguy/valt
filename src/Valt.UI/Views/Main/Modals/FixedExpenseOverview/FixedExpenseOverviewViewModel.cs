using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Core.Common;
using Valt.Core.Kernel.Abstractions.Time;
using Valt.Core.Modules.Budget.FixedExpenses;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Budget.FixedExpenses;
using Valt.Infra.Settings;
using Valt.UI.Base;
using Valt.UI.Helpers;
using Valt.UI.Lang;
using Valt.UI.State;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Modals.FixedExpenseOverview;

public partial class FixedExpenseOverviewViewModel : ValtModalViewModel
{
    private readonly IFixedExpenseProvider _fixedExpenseProvider = null!;
    private readonly RatesState _ratesState = null!;
    private readonly CurrencySettings _currencySettings = null!;
    private readonly IClock _clock = null!;
    private readonly SecureModeState _secureModeState = null!;

    [ObservableProperty] private AvaloniaList<MonthGroupViewModel> _monthGroups = new();
    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private AvaloniaList<int> _availableYears = new();
    [ObservableProperty] private string _paidTotal = string.Empty;
    [ObservableProperty] private string _futureExpensesTotal = string.Empty;

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public FixedExpenseOverviewViewModel()
    {
        if (!Design.IsDesignMode) return;

        SelectedYear = 2026;
        AvailableYears.Add(2026);
    }

    public FixedExpenseOverviewViewModel(
        IFixedExpenseProvider fixedExpenseProvider,
        RatesState ratesState,
        CurrencySettings currencySettings,
        IClock clock,
        SecureModeState secureModeState)
    {
        _fixedExpenseProvider = fixedExpenseProvider;
        _ratesState = ratesState;
        _currencySettings = currencySettings;
        _clock = clock;
        _secureModeState = secureModeState;

        var currentYear = _clock.GetCurrentLocalDate().Year;
        for (var y = currentYear - 2; y <= currentYear + 1; y++)
            AvailableYears.Add(y);

        _selectedYear = currentYear;

        _ = LoadDataAsync();
    }

    partial void OnSelectedYearChanged(int value)
    {
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var currentDate = _clock.GetCurrentLocalDate();
        var fixedExpenseHelper = new FixedExpenseHelper(_ratesState, _currencySettings);
        var mainCurrency = _currencySettings.MainFiatCurrency;

        var allEntries = new List<(int month, IEnumerable<FixedExpenseProviderEntry> entries)>();

        for (var month = 1; month <= 12; month++)
        {
            var date = new DateOnly(SelectedYear, month, 1);
            var entries = await _fixedExpenseProvider.GetFixedExpensesOfMonthAsync(date);
            allEntries.Add((month, entries));
        }

        var monthGroupsList = new List<MonthGroupViewModel>();
        var paidTotalAmount = 0m;
        var futureMinTotal = 0m;
        var futureMaxTotal = 0m;

        foreach (var (month, entries) in allEntries)
        {
            var entryList = entries.ToList();
            var overviewEntries = new AvaloniaList<OverviewEntryViewModel>();

            foreach (var entry in entryList)
            {
                var statusText = GetStatusText(entry.State);
                var statusColor = GetStatusColor(entry);

                var expectedAmount = FormatExpectedAmount(entry);

                string actualAmount;
                var isOutOfRange = false;
                SolidColorBrush actualAmountColor;

                if (entry.Paid && entry.ActualAmount.HasValue)
                {
                    var absActualAmount = Math.Abs(entry.ActualAmount.Value);
                    actualAmount = CurrencyDisplay.FormatFiat(absActualAmount, entry.Currency);
                    isOutOfRange = IsAmountOutOfRange(entry);
                    actualAmountColor = isOutOfRange
                        ? FixedExpenseListResources.WarningForeground
                        : FixedExpenseListResources.DefaultForeground;

                    var (convertedMin, _) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        absActualAmount, null, null, entry.Currency);
                    paidTotalAmount += convertedMin;
                }
                else if (entry.MarkedAsPaid)
                {
                    actualAmount = "-";
                    actualAmountColor = FixedExpenseListResources.DefaultForeground;

                    // For manually paid, use expected amount as paid estimate
                    var (convertedMin, convertedMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        entry.FixedAmount, entry.RangedAmountMin, entry.RangedAmountMax, entry.Currency);
                    paidTotalAmount += (convertedMin + convertedMax) / 2;
                }
                else
                {
                    actualAmount = "-";
                    actualAmountColor = FixedExpenseListResources.DefaultForeground;
                }

                // Future expenses: empty state and reference date is in the future (not ignored)
                if (entry.Empty && entry.ReferenceDate >= currentDate)
                {
                    var (convertedMin, convertedMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        entry.FixedAmount, entry.RangedAmountMin, entry.RangedAmountMax, entry.Currency);
                    futureMinTotal += convertedMin;
                    futureMaxTotal += convertedMax;
                }

                overviewEntries.Add(new OverviewEntryViewModel
                {
                    Name = entry.Name,
                    Day = entry.Day,
                    DayFormatted = entry.Day.ToString().PadLeft(2, '0'),
                    StatusText = statusText,
                    StatusColor = statusColor,
                    ExpectedAmount = expectedAmount,
                    ActualAmount = actualAmount,
                    ActualAmountColor = actualAmountColor,
                    IsOutOfRange = isOutOfRange
                });
            }

            // Per-month totals
            var monthPaidAmount = 0m;
            var monthExpectedMin = 0m;
            var monthExpectedMax = 0m;

            foreach (var entry in entryList)
            {
                // Paid total for the month
                if (entry.Paid && entry.ActualAmount.HasValue)
                {
                    var (convertedMin, _) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        Math.Abs(entry.ActualAmount.Value), null, null, entry.Currency);
                    monthPaidAmount += convertedMin;
                }
                else if (entry.MarkedAsPaid)
                {
                    var (convertedMin, convertedMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        entry.FixedAmount, entry.RangedAmountMin, entry.RangedAmountMax, entry.Currency);
                    monthPaidAmount += (convertedMin + convertedMax) / 2;
                }

                // Expected total for the month (all entries, regardless of state)
                if (!entry.Ignored)
                {
                    var (convertedMin, convertedMax) = fixedExpenseHelper.CalculateFixedExpenseRange(
                        entry.FixedAmount, entry.RangedAmountMin, entry.RangedAmountMax, entry.Currency);
                    monthExpectedMin += convertedMin;
                    monthExpectedMax += convertedMax;
                }
            }

            string monthExpectedRange;
            if (monthExpectedMin == monthExpectedMax)
                monthExpectedRange = CurrencyDisplay.FormatFiat(monthExpectedMin, mainCurrency);
            else
                monthExpectedRange = $"{CurrencyDisplay.FormatFiat(monthExpectedMin, mainCurrency)} - {CurrencyDisplay.FormatFiat(monthExpectedMax, mainCurrency)}";

            var monthDate = new DateOnly(SelectedYear, month, 1);
            var monthName = monthDate.ToString("MMMM", CultureInfo.CurrentCulture);
            monthName = char.ToUpper(monthName[0]) + monthName[1..];

            monthGroupsList.Add(new MonthGroupViewModel
            {
                MonthName = monthName,
                Entries = overviewEntries,
                HasEntries = overviewEntries.Count > 0,
                MonthPaidTotal = _secureModeState.IsEnabled ? "---" : CurrencyDisplay.FormatFiat(monthPaidAmount, mainCurrency),
                MonthExpectedRange = _secureModeState.IsEnabled ? "---" : monthExpectedRange
            });
        }

        MonthGroups = new AvaloniaList<MonthGroupViewModel>(monthGroupsList);

        if (_secureModeState.IsEnabled)
        {
            PaidTotal = "---";
            FutureExpensesTotal = "---";
        }
        else
        {
            PaidTotal = CurrencyDisplay.FormatFiat(paidTotalAmount, mainCurrency);

            if (futureMinTotal == futureMaxTotal)
            {
                FutureExpensesTotal = CurrencyDisplay.FormatFiat(futureMinTotal, mainCurrency);
            }
            else
            {
                var middle = (futureMinTotal + futureMaxTotal) / 2;
                FutureExpensesTotal = $"~ {CurrencyDisplay.FormatFiat(middle, mainCurrency)}";
            }
        }
    }

    private static string GetStatusText(FixedExpenseRecordState state)
    {
        return state switch
        {
            FixedExpenseRecordState.Paid => language.FixedExpenseOverview_Status_Paid,
            FixedExpenseRecordState.Ignored => language.FixedExpenseOverview_Status_Ignored,
            FixedExpenseRecordState.ManuallyPaid => language.FixedExpenseOverview_Status_ManuallyPaid,
            _ => language.FixedExpenseOverview_Status_Pending
        };
    }

    private static SolidColorBrush GetStatusColor(FixedExpenseProviderEntry entry)
    {
        if (entry.Paid || entry.MarkedAsPaid)
            return FixedExpenseListResources.PaidForeground;

        if (entry.Ignored)
            return FixedExpenseListResources.IgnoredForeground;

        return FixedExpenseListResources.DefaultForeground;
    }

    private static string FormatExpectedAmount(FixedExpenseProviderEntry entry)
    {
        if (entry.FixedAmount.HasValue)
            return CurrencyDisplay.FormatFiat(entry.FixedAmount.Value, entry.Currency);

        if (entry.RangedAmountMin.HasValue && entry.RangedAmountMax.HasValue)
            return $"{CurrencyDisplay.FormatFiat(entry.RangedAmountMin.Value, entry.Currency)} - {CurrencyDisplay.FormatFiat(entry.RangedAmountMax.Value, entry.Currency)}";

        return string.Empty;
    }

    private static bool IsAmountOutOfRange(FixedExpenseProviderEntry entry)
    {
        if (!entry.ActualAmount.HasValue)
            return false;

        var actual = Math.Abs(entry.ActualAmount.Value);

        if (entry.FixedAmount.HasValue)
            return actual != entry.FixedAmount.Value;

        if (entry.RangedAmountMin.HasValue && entry.RangedAmountMax.HasValue)
            return actual < entry.RangedAmountMin.Value || actual > entry.RangedAmountMax.Value;

        return false;
    }

    [RelayCommand]
    private Task Close()
    {
        CloseWindow?.Invoke();
        return Task.CompletedTask;
    }

    public record MonthGroupViewModel
    {
        public string MonthName { get; init; } = string.Empty;
        public AvaloniaList<OverviewEntryViewModel> Entries { get; init; } = new();
        public bool HasEntries { get; init; }
        public string MonthPaidTotal { get; init; } = string.Empty;
        public string MonthExpectedRange { get; init; } = string.Empty;
    }

    public record OverviewEntryViewModel
    {
        public string Name { get; init; } = string.Empty;
        public int Day { get; init; }
        public string DayFormatted { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public SolidColorBrush StatusColor { get; init; } = new(Colors.Gray);
        public string ExpectedAmount { get; init; } = string.Empty;
        public string ActualAmount { get; init; } = string.Empty;
        public SolidColorBrush ActualAmountColor { get; init; } = new(Colors.Gray);
        public bool IsOutOfRange { get; init; }
    }

    public record Response(bool Ok);
}
