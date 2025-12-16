using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.UI.Lang;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Reports.Models;

public class MonthlyReportItemViewModel : ObservableObject
{
    public MonthlyReportItemViewModel(FiatCurrency currency, MonthlyTotalsData.Item item)
    {
        Currency = currency;
        MonthYear = item.MonthYear;
        BtcTotal = item.BtcTotal;
        BtcMonthlyChange = item.BtcMonthlyChange;
        BtcYearlyChange = item.BtcYearlyChange;
        FiatTotal = item.FiatTotal;
        FiatMonthlyChange = item.FiatMonthlyChange;
        FiatYearlyChange = item.FiatYearlyChange;
        Income = item.Income;
        Expenses = item.Expenses;
        BitcoinPurchased = item.BitcoinPurchased;
        BitcoinSold = item.BitcoinSold;
        BitcoinIncome = item.BitcoinIncome;
        BitcoinExpenses = item.BitcoinExpenses;
    }
    
    public MonthlyReportItemViewModel(FiatCurrency currency, MonthlyTotalsData.Totals item)
    {
        Currency = currency;
        Income = item.Income;
        Expenses = item.Expenses;
        BitcoinPurchased = item.BitcoinPurchased;
        BitcoinSold = item.BitcoinSold;
        BitcoinIncome = item.BitcoinIncome;
        BitcoinExpenses = item.BitcoinExpenses;
    }

    public FiatCurrency Currency { get; init; }

    public DateOnly? MonthYear { get; init; }
    public string MonthYearFormatted => MonthYear?.ToString("MMMM yyyy") ?? language.Total;
    public decimal? BtcTotal { get; init; }

    public string BtcTotalFormatted =>
        BtcTotal is not null ? CurrencyDisplay.FormatAsBitcoin(BtcTotal.Value) : string.Empty;

    public decimal? BtcMonthlyChange { get; init; }

    public string BtcMonthlyChangeFormatted => BtcMonthlyChange is not null
        ? BtcMonthlyChange >= 0 ? $"+{BtcMonthlyChange}%" : $"{BtcMonthlyChange}%"
        : string.Empty;

    public SolidColorBrush BtcMonthlyChangeColor => BtcMonthlyChange is not null
        ? Process(BtcMonthlyChange.Value)
        : TransactionGridResources.Credit;

    public decimal? BtcYearlyChange { get; init; }

    public string BtcYearlyChangeFormatted => BtcMonthlyChange is not null
        ? BtcYearlyChange >= 0 ? $"+{BtcYearlyChange}%" : $"{BtcYearlyChange}%"
        : string.Empty;

    public SolidColorBrush BtcYearlyChangeColor => BtcYearlyChange is not null
        ? Process(BtcYearlyChange.Value)
        : TransactionGridResources.Credit;

    public decimal? FiatTotal { get; init; }

    public string FiatTotalFormatted => FiatTotal is not null
        ? CurrencyDisplay.FormatFiat(FiatTotal.Value, Currency.Code)
        : string.Empty;

    public decimal? FiatMonthlyChange { get; init; }

    public string FiatMonthlyChangeFormatted => FiatMonthlyChange is not null
        ? FiatMonthlyChange >= 0 ? $"+{FiatMonthlyChange}%" : $"{FiatMonthlyChange}%"
        : string.Empty;

    public SolidColorBrush FiatMonthlyChangeColor => FiatMonthlyChange is not null
        ? Process(FiatMonthlyChange.Value)
        : TransactionGridResources.Credit;

    public decimal? FiatYearlyChange { get; init; }

    public string FiatYearlyChangeFormatted => FiatYearlyChange is not null
        ? FiatYearlyChange >= 0 ? $"+{FiatYearlyChange}%" : $"{FiatYearlyChange}%"
        : string.Empty;

    public SolidColorBrush FiatYearlyChangeColor => FiatYearlyChange is not null
        ? Process(FiatYearlyChange.Value)
        : TransactionGridResources.Credit;

    public decimal Income { get; init; }
    public string IncomeFormatted => CurrencyDisplay.FormatFiat(Income, Currency.Code);
    public decimal Expenses { get; init; }
    public string ExpensesFormatted => CurrencyDisplay.FormatFiat(Expenses, Currency.Code);

    public decimal BitcoinPurchased { get; init; }
    public string BitcoinPurchasedFormatted => CurrencyDisplay.FormatAsBitcoin(BitcoinPurchased);
    public decimal BitcoinSold { get; init; }
    public string BitcoinSoldFormatted => CurrencyDisplay.FormatAsBitcoin(BitcoinSold);

    public decimal BitcoinIncome { get; init; }
    public string BitcoinIncomeFormatted => CurrencyDisplay.FormatAsBitcoin(BitcoinIncome);
    public decimal BitcoinExpenses { get; init; }
    public string BitcoinExpensesFormatted => CurrencyDisplay.FormatAsBitcoin(BitcoinExpenses);

    private SolidColorBrush Process(decimal percentage)
    {
        return percentage >= 0 ? TransactionGridResources.Credit : TransactionGridResources.Debt;
    }
}