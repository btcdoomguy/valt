using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class LoanCostChartData : ReportChartDataBase
{
    private static readonly SKColor CostColor = SKColor.Parse("#0566e9");        // Secondary500

    public string PrimaryCurrency { get; set; } = FiatCurrency.Usd.Code;

    public ObservableCollection<double> CostValues { get; } = new();

    private StackedColumnSeries<double>? _costSeries;

    public LoanCostChartData()
    {
        YAxes[0] = CreateValueAxis(FiatLabeler, minLimit: 0);
    }

    private string FiatLabeler(double value)
    {
        return CurrencyDisplay.FormatFiat((decimal)value, PrimaryCurrency);
    }

    private StackedColumnSeries<double> CreateCostSeries() => new()
    {
        Name = language.Reports_LoanReports_CostSeries,
        Values = CostValues,
        Stroke = null,
        Fill = new SolidColorPaint(CostColor)
    };

    public void RefreshChart(LoanReportsDataDto data)
    {
        PrimaryCurrency = data.PrimaryCurrency;

        CostValues.Clear();
        MonthLabels.Clear();

        BeginSeriesRebuild();

        foreach (var month in data.CostMonths)
        {
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            CostValues.Add((double)month.CombinedCost);
        }

        _costSeries = CreateCostSeries();
        Series.Add(_costSeries);
    }

    protected override void DisposeSeries()
    {
        DisposePaints(_costSeries?.Fill);
        _costSeries = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        CostValues.Clear();
    }
}
