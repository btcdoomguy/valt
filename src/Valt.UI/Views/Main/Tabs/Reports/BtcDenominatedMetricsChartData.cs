using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.BtcDenominatedMetrics.DTOs;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class BtcDenominatedMetricsChartData : IDisposable
{
    private static readonly SKColor EarnedColor = SKColor.Parse("#06e806");       // SemanticPositive500
    private static readonly SKColor SpentColor = SKColor.Parse("#e80606");        // SemanticNegative500

    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    private const int MinChartHeight = 300;
    private const int HeightPerCategory = 40;

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();
    public ObservableCollection<string> CategoryLabels { get; } = new();
    public ObservableCollection<double> EarnedValues { get; } = new();
    public ObservableCollection<double> SpentValues { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];
    public Axis[] CategoryXAxes { get; } = new Axis[1];
    public Axis[] CategoryYAxes { get; } = new Axis[1];

    public double ChartHeight { get; private set; } = MinChartHeight;

    private StackedColumnSeries<double>? _earnedSeries;
    private StackedColumnSeries<double>? _spentSeries;
    private RowSeries<double>? _categoryRowSeries;

    public BtcDenominatedMetricsChartData()
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

        CategoryXAxes[0] = new Axis
        {
            Labeler = SatsLabeler,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinZoomDelta = 1
        };

        CategoryYAxes[0] = new Axis
        {
            Labels = CategoryLabels,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinStep = 1,
            ForceStepToMin = true
        };
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

        // Reset X-axis zoom to show all data points when year changes
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        // Dispose and detach the previous series so LiveCharts rebuilds the columns
        // from scratch — reusing the same series instance leaves stale geometry
        // after a window resize combined with a data refresh.
        DisposeSeries();
        Series.Clear();

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

    public void RefreshCategoryChart(IReadOnlyList<SatsSpentByCategoryDto> items)
    {
        CategoryLabels.Clear();

        DisposeSeries();
        Series.Clear();

        // Sort by magnitude descending (largest spenders first) for the row chart
        var sortedItems = items
            .OrderByDescending(x => x.SatsTotal)
            .ToList();

        ChartHeight = Math.Max(MinChartHeight, sortedItems.Count * HeightPerCategory);

        foreach (var item in sortedItems)
        {
            CategoryLabels.Add(item.CategoryName);
        }

        var values = sortedItems.Select(x => (double)x.SatsTotal).ToList();
        var colors = sortedItems
            .Select(x =>
            {
                if (string.IsNullOrWhiteSpace(x.IconColor) || !int.TryParse(x.IconColor, System.Globalization.NumberStyles.HexNumber, null, out var argb))
                    return TextColor;

                return new SKColor((uint)argb).WithAlpha(255);
            })
            .ToList();

        _categoryRowSeries = new RowSeries<double>
        {
            Values = values,
            DataLabelsPaint = new SolidColorPaint(SKColors.White) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            DataLabelsSize = 12,
            DataLabelsPosition = DataLabelsPosition.End,
            DataLabelsFormatter = point => CurrencyDisplay.FormatSatsAsNumber((long)(double)point.Model),
            XToolTipLabelFormatter = point => CurrencyDisplay.FormatSatsAsNumber((long)(double)point.Model),
            YToolTipLabelFormatter = _ => null!,
            Padding = 2
        };

        _categoryRowSeries.PointMeasured += point =>
        {
            if (point.Visual is null) return;
            var index = point.Index;
            if (index < colors.Count)
                point.Visual.Fill = new SolidColorPaint(colors[index]);
        };

        Series.Add(_categoryRowSeries);
    }

    private void DisposeSeries()
    {
        if (_earnedSeries is not null)
        {
            (_earnedSeries.Fill as IDisposable)?.Dispose();
            _earnedSeries = null;
        }

        if (_spentSeries is not null)
        {
            (_spentSeries.Fill as IDisposable)?.Dispose();
            _spentSeries = null;
        }

        if (_categoryRowSeries is not null)
        {
            (_categoryRowSeries.Fill as IDisposable)?.Dispose();
            (_categoryRowSeries.DataLabelsPaint as IDisposable)?.Dispose();
            _categoryRowSeries = null;
        }
    }

    public void Dispose()
    {
        (LegendTextPaint as IDisposable)?.Dispose();
        (TooltipTextPaint as IDisposable)?.Dispose();
        (TooltipBackgroundPaint as IDisposable)?.Dispose();
        DisposeSeries();
        Series.Clear();
        MonthLabels.Clear();
        CategoryLabels.Clear();
        EarnedValues.Clear();
        SpentValues.Clear();
    }
}
