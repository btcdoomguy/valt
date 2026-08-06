using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class StackVelocityChartData : IDisposable
{
    private static readonly SKColor AccentPrimary = SKColor.Parse("#e98805");       // Accent500
    private static readonly SKColor AccentLight = SKColor.Parse("#ffa122");       // Accent400
    private static readonly SKColor AccentFill = SKColor.Parse("#ffa122").WithAlpha(40);

    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ObservablePoint> VelocityValues { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];

    public ObservableCollection<ISeries> Series { get; } = new();

    private LineSeries<ObservablePoint>? _velocitySeries;

    public StackVelocityChartData()
    {
        XAxes[0] =
            new Axis
            {
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(60)) { StrokeThickness = 1 },
                LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
                TextSize = 12,
                Position = AxisPosition.End,
                Labels = MonthLabels,
                LabelsRotation = -45,
                MinZoomDelta = 1
            };

        YAxes[0] = new Axis
        {
            Labeler = SatsLabeler,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinZoomDelta = 1
        };
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

        // Reset X-axis zoom to show all data points when year changes
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        // Dispose and detach the previous series so LiveCharts rebuilds the line
        // path from scratch — reusing the same series instance leaves stale geometry
        // after a window resize combined with a data refresh.
        DisposeSeries();
        Series.Clear();

        for (var index = 0; index < data.Months.Count; index++)
        {
            var month = data.Months[index];
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            VelocityValues.Add(new ObservablePoint(index, (double)month.StackVelocity));
        }

        _velocitySeries = CreateVelocitySeries();
        Series.Add(_velocitySeries);
    }

    private void DisposeSeries()
    {
        if (_velocitySeries is not null)
        {
            (_velocitySeries.Stroke as IDisposable)?.Dispose();
            (_velocitySeries.GeometryStroke as IDisposable)?.Dispose();
            (_velocitySeries.GeometryFill as IDisposable)?.Dispose();
            (_velocitySeries.Fill as IDisposable)?.Dispose();
            _velocitySeries = null;
        }
    }

    public void Dispose()
    {
        (LegendTextPaint as IDisposable)?.Dispose();
        (TooltipTextPaint as IDisposable)?.Dispose();
        (TooltipBackgroundPaint as IDisposable)?.Dispose();
        DisposeSeries();
        Series.Clear();
        VelocityValues.Clear();
        MonthLabels.Clear();
    }
}
