using Valt.Core.Common;
using Valt.Infra.Modules.Reports.MonthlyTotals;
using Valt.UI.Views.Main.Tabs.Reports;

namespace Valt.Tests.UI;

[TestFixture]
public class MonthlyTotalsChartDataTests
{
    [Test]
    public void RefreshChart_With_Items_Carrying_AssetsTotalFiat_Should_Populate_One_Assets_Point_Per_Item()
    {
        var chartData = new MonthlyTotalsChartData();
        var data = new MonthlyTotalsData
        {
            MainCurrency = FiatCurrency.Usd,
            Items =
            [
                CreateItem(new DateOnly(2026, 1, 31), fiatTotal: 1000m, assetsTotalFiat: 250m),
                CreateItem(new DateOnly(2026, 2, 28), fiatTotal: 2000m, assetsTotalFiat: 400m),
                CreateItem(new DateOnly(2026, 3, 31), fiatTotal: 3000m, assetsTotalFiat: 0m)
            ],
            Total = CreateTotals()
        };

        chartData.RefreshChart(data);

        Assert.That(chartData.AssetsValues, Has.Count.EqualTo(3));
        Assert.That(chartData.AssetsValues[0].X, Is.EqualTo(0));
        Assert.That(chartData.AssetsValues[0].Y, Is.EqualTo(250d));
        Assert.That(chartData.AssetsValues[1].X, Is.EqualTo(1));
        Assert.That(chartData.AssetsValues[1].Y, Is.EqualTo(400d));
        Assert.That(chartData.AssetsValues[2].Y, Is.EqualTo(0d));

        chartData.Dispose();
    }

    [Test]
    public void RefreshChart_Should_Add_Three_Series_And_Dispose_Should_Clear_All_Without_Leaking()
    {
        var chartData = new MonthlyTotalsChartData();
        var data = new MonthlyTotalsData
        {
            MainCurrency = FiatCurrency.Usd,
            Items = [CreateItem(new DateOnly(2026, 1, 31), fiatTotal: 1000m, assetsTotalFiat: 250m)],
            Total = CreateTotals()
        };

        chartData.RefreshChart(data);

        Assert.That(chartData.Series, Has.Count.EqualTo(3));

        chartData.RefreshChart(data);

        Assert.That(chartData.Series, Has.Count.EqualTo(3), "re-refresh must rebuild, not accumulate, series");

        chartData.Dispose();

        Assert.That(chartData.Series, Is.Empty);
        Assert.That(chartData.FiatValues, Is.Empty);
        Assert.That(chartData.BtcValues, Is.Empty);
        Assert.That(chartData.AssetsValues, Is.Empty);
    }

    private static MonthlyTotalsData.Item CreateItem(DateOnly monthYear, decimal fiatTotal, decimal assetsTotalFiat) => new()
    {
        MonthYear = monthYear,
        BtcTotal = 0.1m,
        BtcMonthlyChange = 0m,
        BtcYearlyChange = 0m,
        FiatTotal = fiatTotal,
        FiatMonthlyChange = 0m,
        FiatYearlyChange = 0m,
        Income = 0m,
        Expenses = 0m,
        BitcoinPurchased = 0m,
        BitcoinSold = 0m,
        BitcoinIncome = 0m,
        BitcoinExpenses = 0m,
        AllIncomeInFiat = 0m,
        AllExpensesInFiat = 0m,
        AssetsTotalFiat = assetsTotalFiat,
        AssetsTotalSats = 0L
    };

    private static MonthlyTotalsData.Totals CreateTotals() => new()
    {
        Income = 0m,
        Expenses = 0m,
        BitcoinPurchased = 0m,
        BitcoinSold = 0m,
        BitcoinIncome = 0m,
        BitcoinExpenses = 0m,
        AllIncomeInFiat = 0m,
        AllExpensesInFiat = 0m
    };
}
