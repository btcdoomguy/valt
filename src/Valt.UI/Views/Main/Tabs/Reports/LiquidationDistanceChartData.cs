using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.UI.Views.Main.Tabs.Transactions.Models;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class LiquidationDistanceChartData : IDisposable
{
    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ObservablePoint> DistanceValues { get; } = new();
    public ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];
    public Axis[] YAxes { get; } = new Axis[1];

    public ObservableCollection<ISeries> Series { get; } = new();

    private LineSeries<ObservablePoint>? _distanceSeries;

    public LiquidationDistanceChartData()
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
            Labeler = PercentageLabeler,
            LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinLimit = 0,
            MinZoomDelta = 1
        };
    }

    private static string PercentageLabeler(double value)
    {
        return $"{value:F0} pp";
    }

    private static SKColor GetRiskColor(decimal distance)
    {
        SolidColorBrush brush;
        if (distance > 15m)
            brush = TransactionGridResources.Credit;
        else if (distance > 0m)
            brush = TransactionGridResources.Warning;
        else
            brush = TransactionGridResources.Debt;

        var color = brush.Color;
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    private LineSeries<ObservablePoint> CreateDistanceSeries(SKColor strokeColor)
    {
        var fillColor = strokeColor.WithAlpha(40);
        var lightColor = strokeColor.WithAlpha(180);

        return new LineSeries<ObservablePoint>
        {
            Name = language.Reports_LoanReports_DistanceSeries,
            Values = DistanceValues,
            Stroke = new SolidColorPaint(strokeColor) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(lightColor) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(lightColor),
            GeometrySize = 8,
            Fill = new SolidColorPaint(fillColor),
            LineSmoothness = 0.3
        };
    }

    public void RefreshChart(LoanReportsDataDto data)
    {
        DistanceValues.Clear();
        MonthLabels.Clear();

        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        DisposeSeries();
        Series.Clear();

        if (data.DistanceMonths.Count == 0)
            return;

        var minDistance = data.DistanceMonths.Min(m => m.DistanceToLiquidation);
        var riskColor = GetRiskColor(minDistance);

        for (var index = 0; index < data.DistanceMonths.Count; index++)
        {
            var month = data.DistanceMonths[index];
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            DistanceValues.Add(new ObservablePoint(index, (double)month.DistanceToLiquidation));
        }

        _distanceSeries = CreateDistanceSeries(riskColor);
        Series.Add(_distanceSeries);
    }

    private void DisposeSeries()
    {
        if (_distanceSeries is not null)
        {
            (_distanceSeries.Stroke as IDisposable)?.Dispose();
            (_distanceSeries.GeometryStroke as IDisposable)?.Dispose();
            (_distanceSeries.GeometryFill as IDisposable)?.Dispose();
            (_distanceSeries.Fill as IDisposable)?.Dispose();
            _distanceSeries = null;
        }
    }

    public void Dispose()
    {
        (LegendTextPaint as IDisposable)?.Dispose();
        (TooltipTextPaint as IDisposable)?.Dispose();
        (TooltipBackgroundPaint as IDisposable)?.Dispose();
        DisposeSeries();
        Series.Clear();
        DistanceValues.Clear();
        MonthLabels.Clear();
    }
}
