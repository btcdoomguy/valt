using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class SavingsRateChartData : IDisposable
{
    // Color palette - Bitcoin (Orange shades from Accent)
    private static readonly SKColor BtcPrimary = SKColor.Parse("#ffa122");       // Accent400
    private static readonly SKColor BtcLight = SKColor.Parse("#ffcc88");         // Accent200
    private static readonly SKColor BtcDark = SKColor.Parse("#e98805");          // Accent500
    private static readonly SKColor BtcFill = SKColor.Parse("#ffa122").WithAlpha(40);

    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    // Chart styling
    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);
    public ObservableCollection<ObservablePoint> RateValues { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];

    public Axis[] YAxes { get; } = new Axis[1];

    public ObservableCollection<ISeries> Series { get; } = new();

    private LineSeries<ObservablePoint>? _rateSeries;

    public SavingsRateChartData()
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
            Labeler = RateLabeler,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinZoomDelta = 1
        };
    }

    private static string RateLabeler(double value)
    {
        return $"{value:F0}%";
    }

    private LineSeries<ObservablePoint> CreateRateSeries() => new()
    {
        Name = language.Reports_SavingsRate_Title,
        Values = RateValues,
        Stroke = new SolidColorPaint(BtcDark) { StrokeThickness = 2.5f },
        GeometryStroke = new SolidColorPaint(BtcPrimary) { StrokeThickness = 2 },
        GeometryFill = new SolidColorPaint(BtcLight),
        GeometrySize = 8,
        Fill = new SolidColorPaint(BtcFill),
        LineSmoothness = 0.3
    };

    public void RefreshChart(SavingsRateDataDto savingsRateData)
    {
        RateValues.Clear();
        MonthLabels.Clear();

        // Reset X-axis zoom to show all data points when year changes
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        // Dispose and detach the previous series so LiveCharts rebuilds the line
        // path from scratch — reusing the same series instance leaves stale geometry
        // after a window resize combined with a data refresh.
        DisposeSeries();
        Series.Clear();

        for (var index = 0; index < savingsRateData.Months.Count; index++)
        {
            var month = savingsRateData.Months[index];
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            RateValues.Add(new ObservablePoint(index, month.Rate.HasValue ? (double?)month.Rate.Value : null));
        }

        _rateSeries = CreateRateSeries();
        Series.Add(_rateSeries);
    }

    private void DisposeSeries()
    {
        if (_rateSeries is not null)
        {
            (_rateSeries.Stroke as IDisposable)?.Dispose();
            (_rateSeries.GeometryStroke as IDisposable)?.Dispose();
            (_rateSeries.GeometryFill as IDisposable)?.Dispose();
            (_rateSeries.Fill as IDisposable)?.Dispose();
            _rateSeries = null;
        }
    }

    public void Dispose()
    {
        DisposeSeries();
        Series.Clear();
        RateValues.Clear();
        MonthLabels.Clear();
    }
}
