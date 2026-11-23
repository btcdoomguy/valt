using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.ExpensesByCategory;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class ExpensesByCategoryChartData : IDisposable, INotifyPropertyChanged
{
    private const int MinChartHeight = 300;
    private const int HeightPerCategory = 40;

    public event PropertyChangedEventHandler? PropertyChanged;

    private double _chartHeight = MinChartHeight;
    public double ChartHeight
    {
        get => _chartHeight;
        private set
        {
            if (Math.Abs(_chartHeight - value) > 0.1)
            {
                _chartHeight = value;
                OnPropertyChanged();
            }
        }
    }

    private string _totalText = string.Empty;
    public string TotalText
    {
        get => _totalText;
        private set
        {
            if (_totalText != value)
            {
                _totalText = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    // Chart styling colors
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor TooltipTextColor = SKColor.Parse("#a8a6a4");  // Text400
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKTypeface InterTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal);

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = InterTypeface };
    public SolidColorPaint TooltipTextPaint { get; } = new(TooltipTextColor) { SKTypeface = InterTypeface };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<string> CategoryLabels { get; } = new();

    public FiatCurrency FiatCurrency { get; set; } = FiatCurrency.Usd;

    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }

    private SolidColorPaint[] _barPaints = Array.Empty<SolidColorPaint>();

    public ExpensesByCategoryChartData()
    {
        XAxes = new[]
        {
            new Axis
            {
                Labeler = FiatLabeler,
                LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = InterTypeface },
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(60)) { StrokeThickness = 1 },
                MinLimit = 0,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 80, 0)  // Right padding for data labels
            }
        };

        YAxes = new[]
        {
            new Axis
            {
                Labels = CategoryLabels,
                LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = InterTypeface },
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
                MinStep = 1,  // Force showing every label, prevents skipping labels when there are few items
                ForceStepToMin = true
            }
        };
    }

    private string FiatLabeler(double arg)
    {
        return CurrencyDisplay.FormatFiat(-(decimal)arg, FiatCurrency.Code);
    }

    public void RefreshChart(ExpensesByCategoryData expensesByCategoryData)
    {
        // Dispose old series before clearing
        DisposeSeries();
        Series.Clear();
        CategoryLabels.Clear();
        TotalText = string.Empty;

        FiatCurrency = expensesByCategoryData.MainCurrency;

        // Sort items by value descending for better visualization
        var sortedItems = expensesByCategoryData.Items
            .OrderBy(x => x.FiatTotal)
            .ToList();

        // Calculate dynamic height based on number of categories
        ChartHeight = Math.Max(MinChartHeight, sortedItems.Count * HeightPerCategory);

        // Add category labels for Y axis
        foreach (var item in sortedItems)
        {
            CategoryLabels.Add(item.CategoryName);
        }

        // Create a single RowSeries with all values
        var values = sortedItems.Select(x => Convert.ToDouble(x.FiatTotal)).ToList();
        var total = values.Sum();
        TotalText = CurrencyDisplay.FormatFiat(-(decimal)total, FiatCurrency.Code);
        var paints = sortedItems.Select(x =>
        {
            var iconColor = x.Icon.Color;
            return new SolidColorPaint(new SKColor(iconColor.R, iconColor.G, iconColor.B, iconColor.A));
        }).ToArray();
        _barPaints = paints;

        var rowSeries = new RowSeries<double>
        {
            Values = values,
            DataLabelsPaint = new SolidColorPaint(SKColors.White) { SKTypeface = InterTypeface },
            DataLabelsSize = 12,
            DataLabelsPosition = DataLabelsPosition.End,
            DataLabelsFormatter = point =>
            {
                var value = (decimal)point.Model;
                return CurrencyDisplay.FormatFiat(-value, FiatCurrency.Code);
            },
            XToolTipLabelFormatter = point =>
            {
                var value = point.Model;
                var percentage = total > 0 ? (value / total) * 100 : 0;
                var categoryName = point.Index < sortedItems.Count ? sortedItems[point.Index].CategoryName : "";
                var formattedValue = CurrencyDisplay.FormatFiat((decimal)value, FiatCurrency.Code);
                return $"{percentage:F1}%";
            },
            YToolTipLabelFormatter = _ => null!,
            Padding = 2
        };

        // Apply individual colors to each bar
        rowSeries.PointMeasured += point =>
        {
            if (point.Visual is null) return;
            var index = point.Index;
            if (index < paints.Length)
            {
                point.Visual.Fill = paints[index];
            }
        };

        Series.Add(rowSeries);
    }

    private void DisposeSeries()
    {
        foreach (var paint in _barPaints)
        {
            (paint as IDisposable)?.Dispose();
        }
        _barPaints = Array.Empty<SolidColorPaint>();

        foreach (var series in Series)
        {
            if (series is RowSeries<double> rowSeries)
            {
                (rowSeries.Fill as IDisposable)?.Dispose();
                (rowSeries.DataLabelsPaint as IDisposable)?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        DisposeSeries();
        (LegendTextPaint as IDisposable)?.Dispose();
        (TooltipTextPaint as IDisposable)?.Dispose();
        (TooltipBackgroundPaint as IDisposable)?.Dispose();
        foreach (var axis in XAxes.Concat(YAxes))
        {
            (axis.SeparatorsPaint as IDisposable)?.Dispose();
            (axis.LabelsPaint as IDisposable)?.Dispose();
            (axis.NamePaint as IDisposable)?.Dispose();
        }
        Series.Clear();
        CategoryLabels.Clear();
    }
}