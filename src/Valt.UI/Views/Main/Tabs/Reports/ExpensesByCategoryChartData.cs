using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.ExpensesByCategory;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class ExpensesByCategoryChartData : IDisposable
{
    // Chart styling colors
    private static readonly SKColor LegendTextColor = SKColor.Parse("#eeebe8");  // Text200
    private static readonly SKColor TooltipTextColor = SKColor.Parse("#a8a6a4");  // Text400
    private static readonly SKColor ChartBackground = SKColor.Parse("#333333");  // Background800

    public SolidColorPaint LegendTextPaint { get; } = new(LegendTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TooltipTextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(ChartBackground);

    public ObservableCollection<ISeries> Series { get; } = new();
    
    public void RefreshChart(ExpensesByCategoryData expensesByCategoryData)
    {
        // Dispose old series before clearing
        DisposeSeries();
        Series.Clear();

        var data = expensesByCategoryData.Items.Select(x =>
        {
            var fiatTotal = x.FiatTotal;
            var currencyCode = expensesByCategoryData.MainCurrency.Code;
            var formattedValue = CurrencyDisplay.FormatFiat(fiatTotal, currencyCode);

            var iconColor = x.Icon.Color;
            var skColor = new SKColor(iconColor.R, iconColor.G, iconColor.B, iconColor.A);

            return new PieSeries<double>
            {
                Values = new List<double> { Convert.ToDouble(fiatTotal) },
                Name = x.CategoryName,
                Fill = new SolidColorPaint(skColor),

                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 15,
                DataLabelsPosition = PolarLabelsPosition.Outer,

                DataLabelsFormatter = point =>
                    $"{formattedValue}",

                ToolTipLabelFormatter = point => $"{formattedValue}"
            } as ISeries;
        }).ToList();

        foreach (var series in data)
            Series.Add(series);
    }

    private void DisposeSeries()
    {
        foreach (var series in Series)
        {
            if (series is PieSeries<double> pieSeries)
            {
                (pieSeries.Fill as IDisposable)?.Dispose();
                (pieSeries.DataLabelsPaint as IDisposable)?.Dispose();
            }
        }
    }

    public void Dispose()
    {
        DisposeSeries();
        Series.Clear();
    }
}