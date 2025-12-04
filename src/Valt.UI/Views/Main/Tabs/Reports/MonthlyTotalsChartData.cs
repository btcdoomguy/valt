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
    public ObservableCollection<ObservablePoint> FiatValues { get; } = new();
    public ObservableCollection<ObservablePoint> BtcValues { get; } = new();
    private ObservableCollection<string> MonthLabels { get; } = new();

    public Axis[] XAxes { get; } = new Axis[1];

    public Axis[] YAxes { get; } = new Axis[2];

    public MonthlyTotalsChartData()
    {
        XAxes[0] =
            new Axis
            {
                ForceStepToMin = true,
                MinStep = 1,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightSlateGray.WithAlpha(90)) { StrokeThickness = 1 },
                Position = AxisPosition.End,
                Labels = MonthLabels
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
        return CurrencyDisplay.FormatFiat((decimal)arg, FiatCurrency?.Code ?? FiatCurrency.Usd.Code);
    }

    public void RefreshChart(MonthlyTotalsData monthlyTotalsData)
    {
        FiatCurrency = monthlyTotalsData.MainCurrency;
        FiatValues.Clear();
        BtcValues.Clear();
        MonthLabels.Clear();

        for (var index = 0; index < monthlyTotalsData.Items.Count; index++)
        {
            var item = monthlyTotalsData.Items[index];
            
            MonthLabels.Add(item.MonthYear.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            FiatValues.Add(new ObservablePoint(index, (double)item.FiatTotal));
            BtcValues.Add(new ObservablePoint(index, (double)item.BtcTotal));
        }
    }
}