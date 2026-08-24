using System;
using System.Collections.ObjectModel;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Valt.UI.Views.Main.Tabs.Reports;

public abstract class ReportChartDataBase : IDisposable
{
    protected static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    protected static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    protected static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    protected static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; }

    protected ReportChartDataBase(int yAxisCount = 1)
    {
        XAxes[0] = CreateMonthAxis(MonthLabels);
        YAxes = new Axis[yAxisCount];
    }

    protected static Axis CreateMonthAxis(ObservableCollection<string> labels) => new()
    {
        ForceStepToMin = true,
        MinStep = 1,
        SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(60)) { StrokeThickness = 1 },
        LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
        TextSize = 12,
        Position = AxisPosition.End,
        Labels = labels,
        LabelsRotation = -45,
        MinZoomDelta = 1
    };

    protected static Axis CreateValueAxis(Func<double, string> labeler, double? minLimit = null) => new()
    {
        Labeler = labeler,
        LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
        TextSize = 12,
        SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
        MinLimit = minLimit,
        MinZoomDelta = 1
    };

    // Reset zoom and rebuild the series from scratch — reusing the same series
    // instance leaves stale geometry after a window resize combined with a data refresh.
    protected void BeginSeriesRebuild()
    {
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;
        DisposeSeries();
        Series.Clear();
    }

    protected abstract void DisposeSeries();

    protected static void DisposePaints(params Paint?[] paints)
    {
        foreach (var paint in paints)
            (paint as IDisposable)?.Dispose();
    }

    private void DisposeAxisPaints()
    {
        foreach (var axis in XAxes.Concat(YAxes))
        {
            if (axis is null)
                continue;

            DisposePaints(axis.SeparatorsPaint, axis.LabelsPaint, axis.NamePaint);
        }
    }

    public virtual void Dispose()
    {
        DisposePaints(LegendTextPaint, TooltipTextPaint, TooltipBackgroundPaint);
        DisposeAxisPaints();
        DisposeSeries();
        Series.Clear();
        MonthLabels.Clear();
        GC.SuppressFinalize(this);
    }
}
