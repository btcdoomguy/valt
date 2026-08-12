using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class LoanCostChartData : IDisposable
{
    private static readonly SKColor CostColor = SKColor.Parse("#0566e9"); // Secondary500

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
    public ObservableCollection<double> CostValues { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];

    private StackedColumnSeries<double>? _costSeries;

    public LoanCostChartData()
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

        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        DisposeSeries();
        Series.Clear();

        foreach (var month in data.CostMonths)
        {
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            CostValues.Add((double)month.CombinedCost);
        }

        _costSeries = CreateCostSeries();
        Series.Add(_costSeries);
    }

    private void DisposeSeries()
    {
        if (_costSeries is not null)
        {
            (_costSeries.Fill as IDisposable)?.Dispose();
            _costSeries = null;
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
        CostValues.Clear();
    }
}
