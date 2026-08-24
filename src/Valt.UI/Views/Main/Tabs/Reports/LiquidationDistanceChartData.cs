using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.LoanReports.DTOs;
using Valt.UI.Views.Main.Tabs.Transactions.Models;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class LiquidationDistanceChartData : ReportChartDataBase
{
    public ObservableCollection<ObservablePoint> DistanceValues { get; } = new();

    private LineSeries<ObservablePoint>? _distanceSeries;

    public LiquidationDistanceChartData()
    {
        YAxes[0] = CreateValueAxis(PercentageLabeler, minLimit: 0);
    }

    private static string PercentageLabeler(double value)
    {
        return $"{value:F0} pp";
    }

    private static SKColor GetRiskColor(decimal distance)
    {
        Avalonia.Media.SolidColorBrush brush;
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

        BeginSeriesRebuild();

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

    protected override void DisposeSeries()
    {
        DisposePaints(_distanceSeries?.Stroke, _distanceSeries?.GeometryStroke, _distanceSeries?.GeometryFill, _distanceSeries?.Fill);
        _distanceSeries = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        DistanceValues.Clear();
    }
}
