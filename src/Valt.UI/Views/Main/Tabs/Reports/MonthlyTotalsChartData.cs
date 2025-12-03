using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.MonthlyTotals;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class MonthlyTotalsChartData
{
    public FiatCurrency FiatCurrency { get; set; }
    public ObservableCollection<DateTimePoint> FiatValues { get; } = new();
    public ObservableCollection<DateTimePoint> BtcValues  { get; } = new();
    
    public Axis[] XAxes { get; } = new Axis[1];

    public Axis[] YAxes { get; } = new Axis[2];

    public MonthlyTotalsChartData()
    {

        XAxes[0] =
            new DateTimeAxis(TimeSpan.FromDays(27), date => date.ToString("MMM yyyy"))
            {
                MinStep = 27, // Enforces min 27-day step (approx. 1 month) as double (days)
                SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray.WithAlpha(90)) { StrokeThickness = 1 },
                Position = AxisPosition.End
            };

        YAxes[0] = new Axis
        {
            Name = "Fiat",
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),
            Labeler = FiatLabeler,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
        };
        
        YAxes[1] = new Axis
        {
            Name = "Bitcoin",
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),
            Labeler = BitcoinLabeler,
            SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 },
            Position = AxisPosition.End
        };
    }

    private string BitcoinLabeler(double arg)
    {
        return CurrencyDisplay.FormatAsBitcoin((decimal)arg);
    }

    private string FiatLabeler(double arg)
    {
        return CurrencyDisplay.FormatFiat((decimal) arg, FiatCurrency.Code);
    }

    public void RefreshChart(MonthlyTotalsData monthlyTotalsData)
    {
        FiatCurrency = monthlyTotalsData.MainCurrency;
        FiatValues.Clear();
        BtcValues.Clear();

        foreach (var item in monthlyTotalsData.Items)
        {
            var date = item.MonthYear.ToDateTime(new TimeOnly(0, 0));

            FiatValues.Add(new DateTimePoint(date, (double)item.FiatTotal));
            BtcValues.Add(new DateTimePoint(date, (double)item.BtcTotal));
        }
    }
}