using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Valt.Core.Common;
using Valt.Core.Kernel.Factories;
using Valt.Core.Modules.Budget.Categories;
using Valt.Infra.Modules.Reports.ExpensesByCategory;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.UI.State;
using Valt.UI.UserControls;
using Valt.UI.Views.Main.Tabs.Reports.Models;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel
{
    public ReportsViewModel()
    {
        if (!Design.IsDesignMode)
            return;

        SecureModeState = new SecureModeState()
        {
            IsEnabled = false
        };

        FilterMainDate = CategoryFilterMainDate = DateTime.UtcNow.Date;
        FilterRange = new DateRange(DateTime.UtcNow.Date.AddYears(-1), DateTime.UtcNow.Date);
        CategoryFilterRange = new DateRange(new DateTime(CategoryFilterMainDate.Year, CategoryFilterMainDate.Month, 1),
            new DateTime(CategoryFilterMainDate.Year, CategoryFilterMainDate.Month, 1).AddMonths(1).AddDays(-1));

        AllTimeHighData = new DashboardData("Your all-time high", [
            new("ATH (R$):", "R$ 100.000,00"),
            new("Date:", "10/06/2025"),
            new("Decline from ATH:", "-30%")
        ]);

        var monthlyTotalsData = new MonthlyTotalsData()
        {
            MainCurrency = FiatCurrency.Brl,
            Items = new List<MonthlyTotalsData.Item>
            {
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 01, 1),
                    BtcTotal = 12.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 550035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 02, 1),
                    BtcTotal = 6.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 250035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 03, 1),
                    BtcTotal = 8.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 350035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 3, 1),
                    BtcTotal = 15.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 700035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 4, 1),
                    BtcTotal = 17.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 850035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 5, 1),
                    BtcTotal = 9.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 200000.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 6, 1),
                    BtcTotal = 12.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 550035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 7, 1),
                    BtcTotal = 14.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 650035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 8, 1),
                    BtcTotal = 12.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 550035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 9, 1),
                    BtcTotal = 3.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 100000.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 10, 1),
                    BtcTotal = 5.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 250000.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 11, 1),
                    BtcTotal = 12.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 550035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
                new MonthlyTotalsData.Item()
                {
                    MonthYear = new DateOnly(2025, 12, 1),
                    BtcTotal = 19.34567890m,
                    BtcMonthlyChange = 0.01m,
                    BtcYearlyChange = 0.01m,
                    FiatTotal = 950035.51m,
                    FiatMonthlyChange = 0.03m,
                    FiatYearlyChange = 0.03m,
                    Income = 20312.31m,
                    Expenses = 12345.67m,
                    BitcoinExpenses = 0.01m,
                    BitcoinIncome = 0.005m,
                    BitcoinPurchased = 0.03m,
                    BitcoinSold = 0.01m,
                    AllIncomeInFiat = 25123.15m,
                    AllExpensesInFiat = 14021.52m
                },
            },
            Total = new MonthlyTotalsData.Totals()
            {
                BitcoinExpenses = 1000000m,
                BitcoinIncome = 1000000m,
                BitcoinPurchased = 1000000m,
                BitcoinSold = 1000000m,
                Income = 20312.31m,
                Expenses = 12345.67m,
                AllIncomeInFiat = 25123.15m,
                AllExpensesInFiat = 14021.52m
            }
        };

        MonthlyTotalsChartData.RefreshChart(monthlyTotalsData);

        MonthlyReportItems.AddRange(
            monthlyTotalsData.Items.Select(x => new MonthlyReportItemViewModel(FiatCurrency.Brl, x)));

        ExpensesByCategoryChartData.RefreshChart(new ExpensesByCategoryData()
        {
            MainCurrency = FiatCurrency.Brl,
            Items = new List<ExpensesByCategoryData.Item>()
            {
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Refeição",
                    FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Lazer", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Outros", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Teste", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Refeição",
                    FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Lazer", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Outros", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Teste", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Refeição",
                    FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Lazer", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Outros", FiatTotal = 123.45m
                },
                new()
                {
                    CategoryId = IdGenerator.Generate(), Icon = Icon.Empty, CategoryName = "Teste", FiatTotal = 123.45m
                },
            }
        });

        IsAllTimeHighLoading = false;
        IsMonthlyTotalsLoading = false;
        IsSpendingByCategoriesLoading = false;
    }
}