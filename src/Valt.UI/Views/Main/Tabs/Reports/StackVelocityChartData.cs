using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class StackVelocityChartData : ReportChartDataBase
{
    private static readonly SKColor AccentPrimary = SKColor.Parse("#e98805");    // Accent500
    private static readonly SKColor AccentLight = SKColor.Parse("#ffa122");      // Accent400
    private static readonly SKColor AccentFill = SKColor.Parse("#ffa122").WithAlpha(40);

    public ObservableCollection<ObservablePoint> VelocityValues { get; } = new();

    private LineSeries<ObservablePoint>? _velocitySeries;

    public StackVelocityChartData()
    {
        YAxes[0] = CreateValueAxis(SatsLabeler);
    }

    private static string SatsLabeler(double value)
    {
        return CurrencyDisplay.FormatSatsAsNumber((long)value);
    }

    private LineSeries<ObservablePoint> CreateVelocitySeries() => new()
    {
        Name = language.Reports_StackVelocity_Velocity,
        Values = VelocityValues,
        Stroke = new SolidColorPaint(AccentPrimary) { StrokeThickness = 2.5f },
        GeometryStroke = new SolidColorPaint(AccentLight) { StrokeThickness = 2 },
        GeometryFill = new SolidColorPaint(AccentLight),
        GeometrySize = 8,
        Fill = new SolidColorPaint(AccentFill),
        LineSmoothness = 0.3
    };

    public void RefreshChart(BtcDenominatedMetricsDataDto data)
    {
        VelocityValues.Clear();
        MonthLabels.Clear();

        BeginSeriesRebuild();

        for (var index = 0; index < data.Months.Count; index++)
        {
            var month = data.Months[index];
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            VelocityValues.Add(new ObservablePoint(index, (double)month.StackVelocity));
        }

        _velocitySeries = CreateVelocitySeries();
        Series.Add(_velocitySeries);
    }

    protected override void DisposeSeries()
    {
        DisposePaints(_velocitySeries?.Stroke, _velocitySeries?.GeometryStroke, _velocitySeries?.GeometryFill, _velocitySeries?.Fill);
        _velocitySeries = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        VelocityValues.Clear();
    }
}
