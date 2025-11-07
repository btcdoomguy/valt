using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsViewModel : ValtViewModel
{
    [ObservableProperty] private ISeries[] series;

    [ObservableProperty] private Axis[] xAxes;

    [ObservableProperty] private Axis[] yAxes;

    public ReportsViewModel()
    {
        // Sample data: List of (DateOnly, Price). In a real app, this could come from a service or database.
        var rawDataUsd = new ObservableCollection<(DateTime Date, double Price)>
        {
            (new DateTime(2025, 8, 1), 100.50),
            (new DateTime(2025, 8, 5), 105.75),
            (new DateTime(2025, 8, 10), 98.20),
            (new DateTime(2025, 8, 15), 110.00),
            (new DateTime(2025, 8, 20), 115.30)
        };

        var rawDataSats = new ObservableCollection<(DateTime Date, long Sats)>
        {
            (new DateTime(2025, 8, 1), 100000),
            (new DateTime(2025, 8, 5), 90000),
            (new DateTime(2025, 8, 10), 75000),
            (new DateTime(2025, 8, 15), 40000),
            (new DateTime(2025, 8, 20), 10000)
        };

        // Convert to DateTimePoint for charting (DateOnly to DateTime, ignoring time).
        var chartDataUsd = new ObservableCollection<DateTimePoint>();
        foreach (var item in rawDataUsd)
        {
            chartDataUsd.Add(new DateTimePoint(item.Date.Date, item.Price));
        }
        
        var chartDataSats = new ObservableCollection<DateTimePoint>();
        foreach (var item in rawDataSats)
        {
            chartDataSats.Add(new DateTimePoint(item.Date.Date, item.Sats));
        }

        // Define the line series with data binding
        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Values = chartDataUsd,
                Name = "Price in USD",
                Fill = null, // No area fill under the line
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 }, // Custom line color and thickness
                GeometryStroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 }, // Marker outline
                GeometryFill = new SolidColorPaint(SKColors.LightBlue), // Marker fill
                GeometrySize = 8 // Marker size
            },
            new LineSeries<DateTimePoint>
            {
                Values = chartDataSats,
                Name = "Price in BTC",
                Fill = null, // No area fill under the line
                Stroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 }, // Custom line color and thickness
                GeometryStroke = new SolidColorPaint(SKColors.Orange) { StrokeThickness = 3 }, // Marker outline
                GeometryFill = new SolidColorPaint(SKColors.DarkGoldenrod), // Marker fill
                GeometrySize = 8, // Marker size,
                ScalesYAt = 1
            }
        ];

        // X-axis: Date-based (uses ticks every day; adjust TimeSpan for your data spacing)
        XAxes =
        [
            new DateTimeAxis(TimeSpan.FromDays(1), (x) => x.ToShortDateString())
            {
                Name = "Date",
                LabelsPaint = new SolidColorPaint(SKColors.Black), // Label color
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray)
                    { StrokeThickness = 1 } // Grid line color/thickness
            }
        ];

        // Y-axis: Numeric for prices (default works fine, but customizable)
        YAxes =
        [
            new Axis
            {
                Name = "Price ($)",
                Labeler = value => $"{value:C2}", // Format as currency (e.g., $100.50)
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            },
            new Axis
            {
                Name = "Price (Fiat)",
                Labeler = value => value.ToString(),
                LabelsPaint = new SolidColorPaint(SKColors.Black),
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
                Position = AxisPosition.End
            }
        ];
    }
}