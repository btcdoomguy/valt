using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Valt.UI.Views.Main.Modals.PriceHistory;

public class PriceHistoryChartData : IDisposable
{
    private static readonly SKColor BtcPrimary = SKColor.Parse("#ffa122");
    private static readonly SKColor BtcLight = SKColor.Parse("#ffcc88");
    private static readonly SKColor BtcDark = SKColor.Parse("#e98805");
    private static readonly SKColor BtcFill = SKColor.Parse("#ffa122").WithAlpha(40);

    private static readonly SKColor FiatPrimary = SKColor.Parse("#0566e9");
    private static readonly SKColor FiatLight = SKColor.Parse("#559dff");
    private static readonly SKColor FiatDark = SKColor.Parse("#0951b2");
    private static readonly SKColor FiatFill = SKColor.Parse("#0566e9").WithAlpha(40);

    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");

    private const int MaxYears = 5;

    public ObservableCollection<ObservablePoint> Values { get; } = new();
    public ObservableCollection<string> DateLabels { get; } = new();
    public ObservableCollection<ISeries> Series { get; } = new();

    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }

    private LineSeries<ObservablePoint>? _series;

    public PriceHistoryChartData()
    {
        XAxes = new[]
        {
            new Axis
            {
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(60)) { StrokeThickness = 1 },
                LabelsPaint = new SolidColorPaint(SKColors.Transparent),
                TextSize = 0,
                Labels = DateLabels,
                MinZoomDelta = 1,
                ShowSeparatorLines = false
            }
        };

        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
                MinLimit = 0,
                MinZoomDelta = 1
            }
        };
    }

    public void RefreshChart(List<(DateTime Date, decimal Price)> data, bool isBtc)
    {
        Values.Clear();
        DateLabels.Clear();
        DisposeSeries();
        Series.Clear();

        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        // Limit chart data to last 5 years
        var cutoffDate = DateTime.Now.AddYears(-MaxYears);
        var chartData = data.Count > 0
            ? data.Where(d => d.Date >= cutoffDate).ToList()
            : data;

        for (var i = 0; i < chartData.Count; i++)
        {
            DateLabels.Add(chartData[i].Date.ToString("yyyy-MM-dd"));
            Values.Add(new ObservablePoint(i, (double)chartData[i].Price));
        }

        var primary = isBtc ? BtcPrimary : FiatPrimary;
        var light = isBtc ? BtcLight : FiatLight;
        var dark = isBtc ? BtcDark : FiatDark;
        var fill = isBtc ? BtcFill : FiatFill;

        _series = new LineSeries<ObservablePoint>
        {
            Values = Values,
            Stroke = new SolidColorPaint(primary) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(dark) { StrokeThickness = 2 },
            GeometryFill = new SolidColorPaint(light),
            GeometrySize = 0,
            Fill = new SolidColorPaint(fill),
            LineSmoothness = 0.3
        };

        Series.Add(_series);
    }

    private void DisposeSeries()
    {
        if (_series is not null)
        {
            (_series.Stroke as IDisposable)?.Dispose();
            (_series.GeometryStroke as IDisposable)?.Dispose();
            (_series.GeometryFill as IDisposable)?.Dispose();
            (_series.Fill as IDisposable)?.Dispose();
            _series = null;
        }
    }

    public void Dispose()
    {
        DisposeSeries();
        Series.Clear();
        Values.Clear();
        DateLabels.Clear();
    }
}
