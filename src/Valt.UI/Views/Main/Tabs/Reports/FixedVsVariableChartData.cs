using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class FixedVsVariableChartData : IDisposable
{
    private static readonly SKColor FixedColor = SKColor.Parse("#0566e9");       // Secondary500
    private static readonly SKColor VariableColor = SKColor.Parse("#ffa122");      // Accent400

    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    public string PrimaryCurrency { get; set; } = FiatCurrency.Usd.Code;

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();
    public ObservableCollection<double> FixedValues { get; } = new();
    public ObservableCollection<double> VariableValues { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];

    private StackedColumnSeries<double>? _fixedSeries;
    private StackedColumnSeries<double>? _variableSeries;

    public FixedVsVariableChartData()
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
            Labeler = FiatLabeler,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinLimit = 0,
            MinZoomDelta = 1
        };
    }

    private string FiatLabeler(double value)
    {
        return CurrencyDisplay.FormatFiat((decimal)value, PrimaryCurrency);
    }

    private StackedColumnSeries<double> CreateFixedSeries() => new()
    {
        Name = language.Reports_FixedVariable_Fixed,
        Values = FixedValues,
        Stroke = null,
        Fill = new SolidColorPaint(FixedColor)
    };

    private StackedColumnSeries<double> CreateVariableSeries() => new()
    {
        Name = language.Reports_FixedVariable_Variable,
        Values = VariableValues,
        Stroke = null,
        Fill = new SolidColorPaint(VariableColor)
    };

    public void RefreshChart(FixedVsVariableDataDto data)
    {
        PrimaryCurrency = data.PrimaryCurrency;

        FixedValues.Clear();
        VariableValues.Clear();
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
            FixedValues.Add((double)month.FixedTotal);
            VariableValues.Add((double)month.VariableTotal);
        }

        _fixedSeries = CreateFixedSeries();
        _variableSeries = CreateVariableSeries();

        Series.Add(_fixedSeries);
        Series.Add(_variableSeries);
    }

    private void DisposeSeries()
    {
        if (_fixedSeries is not null)
        {
            (_fixedSeries.Fill as IDisposable)?.Dispose();
            _fixedSeries = null;
        }

        if (_variableSeries is not null)
        {
            (_variableSeries.Fill as IDisposable)?.Dispose();
            _variableSeries = null;
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
        FixedValues.Clear();
        VariableValues.Clear();
    }
}
