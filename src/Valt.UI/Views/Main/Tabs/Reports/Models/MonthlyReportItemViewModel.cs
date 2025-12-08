using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Valt.Core.Common;
using Valt.Core.Modules.Budget.Transactions;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.MonthlyTotals;
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
    
    public FiatCurrency Currency { get; init; }

    public DateOnly MonthYear { get; init; }
    public string MonthYearFormatted => MonthYear.ToString("MMMM yyyy");
    public decimal BtcTotal { get; init; }

    public decimal BtcMonthlyChange { get; init; }
    public string BtcMonthlyChangeFormatted => BtcMonthlyChange >= 0 ? $"+{BtcMonthlyChange}%" : $"{BtcMonthlyChange}%";
    public SolidColorBrush BtcMonthlyChangeColor => Process(BtcMonthlyChange);
    
    public decimal BtcYearlyChange { get; init; }
    public string BtcYearlyChangeFormatted => BtcYearlyChange >= 0 ? $"+{BtcYearlyChange}%" : $"{BtcYearlyChange}%";
    public SolidColorBrush BtcYearlyChangeColor => Process(BtcYearlyChange);

    public decimal FiatTotal { get; init; }
    public string FiatTotalFormatted => CurrencyDisplay.FormatFiat(FiatTotal, Currency.Code);

    public decimal FiatMonthlyChange { get; init; }
    public string FiatMonthlyChangeFormatted =>
        FiatMonthlyChange >= 0 ? $"+{FiatMonthlyChange}%" : $"{FiatMonthlyChange}%";
    public SolidColorBrush FiatMonthlyChangeColor => Process(FiatMonthlyChange);

    public decimal FiatYearlyChange { get; init; }
    public string FiatYearlyChangeFormatted => FiatYearlyChange >= 0 ? $"+{FiatYearlyChange}%" : $"{FiatYearlyChange}%";
    public SolidColorBrush FiatYearlyChangeColor => Process(FiatYearlyChange);
    
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