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

public class ExpensesByCategoryChartData
{
    public ObservableCollection<ISeries> Series { get; } = new();
    
    public void RefreshChart(ExpensesByCategoryData expensesByCategoryData)
    {
        Series.Clear();

        var data = expensesByCategoryData.Items.Select(x =>
        {
            var fiatTotal = x.FiatTotal;
            var currencyCode = expensesByCategoryData.MainCurrency.Code;
            var formattedValue = CurrencyDisplay.FormatFiat(fiatTotal, currencyCode);

            return new PieSeries<double>
            {
                Values = new List<double> { Convert.ToDouble(fiatTotal) },
                Name = x.CategoryName,

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
}