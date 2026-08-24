using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class BtcDenominatedMetricsChartData : ReportChartDataBase
{
    private static readonly SKColor EarnedColor = SKColor.Parse("#06e806");      // SemanticPositive500
    private static readonly SKColor SpentColor = SKColor.Parse("#e80606");       // SemanticNegative500

    private const int MinChartHeight = 300;

    public ObservableCollection<double> EarnedValues { get; } = new();
    public ObservableCollection<double> SpentValues { get; } = new();

    public double ChartHeight => MinChartHeight;

    private StackedColumnSeries<double>? _earnedSeries;
    private StackedColumnSeries<double>? _spentSeries;

    public BtcDenominatedMetricsChartData()
    {
        YAxes[0] = CreateValueAxis(SatsLabeler);
    }

    private static string SatsLabeler(double value)
    {
        return CurrencyDisplay.FormatSatsAsNumber((long)value);
    }

    private StackedColumnSeries<double> CreateEarnedSeries() => new()
    {
        Name = language.Reports_BtcMetrics_Earned,
        Values = EarnedValues,
        Stroke = null,
        Fill = new SolidColorPaint(EarnedColor)
    };

    private StackedColumnSeries<double> CreateSpentSeries() => new()
    {
        Name = language.Reports_BtcMetrics_Spent,
        Values = SpentValues,
        Stroke = null,
        Fill = new SolidColorPaint(SpentColor)
    };

    public void RefreshChart(BtcDenominatedMetricsDataDto data)
    {
        EarnedValues.Clear();
        SpentValues.Clear();
        MonthLabels.Clear();

        BeginSeriesRebuild();

        foreach (var month in data.Months)
        {
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            EarnedValues.Add((double)month.SatsEarned);
            SpentValues.Add((double)month.SatsSpent);
        }

        _earnedSeries = CreateEarnedSeries();
        _spentSeries = CreateSpentSeries();

        Series.Add(_earnedSeries);
        Series.Add(_spentSeries);
    }

    protected override void DisposeSeries()
    {
        DisposePaints(_earnedSeries?.Fill);
        _earnedSeries = null;

        DisposePaints(_spentSeries?.Fill);
        _spentSeries = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        EarnedValues.Clear();
        SpentValues.Clear();
    }
}
