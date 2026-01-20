using System;
using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.Infra.Modules.Reports.WealthOverview;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class WealthOverviewChartData : IDisposable
{
    // Color palette - Fiat (Blue shades from Secondary)
    private static readonly SKColor FiatPrimary = SKColor.Parse("#0566e9");      // Secondary500
    private static readonly SKColor FiatLight = SKColor.Parse("#559dff");        // Secondary300
    private static readonly SKColor FiatDark = SKColor.Parse("#0951b2");         // Secondary600
    private static readonly SKColor FiatFill = SKColor.Parse("#0566e9").WithAlpha(40);

    // Color palette - Bitcoin (Orange shades from Accent)
    private static readonly SKColor BtcPrimary = SKColor.Parse("#ffa122");       // Accent400
    private static readonly SKColor BtcLight = SKColor.Parse("#ffcc88");         // Accent200
    private static readonly SKColor BtcDark = SKColor.Parse("#e98805");          // Accent500
    private static readonly SKColor BtcFill = SKColor.Parse("#ffa122").WithAlpha(40);

    // Grid and text colors
    private static readonly SKColor GridColor = SKColor.Parse("#4d4d4d");        // Background700
    private static readonly SKColor TextColor = SKColor.Parse("#a8a6a4");        // Text400

    public FiatCurrency FiatCurrency { get; set; }

    // Chart styling
    public SolidColorPaint LegendTextPaint { get; } = new(SKColor.Parse("#eeebe8")) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipTextPaint { get; } = new(TextColor) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) };
    public SolidColorPaint TooltipBackgroundPaint { get; } = new(SKColor.Parse("#333333"));
    public ObservableCollection<ObservablePoint> FiatValues { get; private set; } = new();
    public ObservableCollection<ObservablePoint> BtcValues { get; private set; } = new();
    public ObservableCollection<string> PeriodLabels { get; private set; } = new();

    public Axis[] XAxes { get; } = new Axis[1];

    public Axis[] YAxes { get; } = new Axis[2];

    public WealthOverviewChartData()
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
                Labels = PeriodLabels,
                LabelsRotation = -45,
                MinZoomDelta = 1
            };

        YAxes[0] = new Axis
        {
            Name = "Fiat",
            NamePaint = new SolidColorPaint(FiatPrimary),
            NameTextSize = 13,
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 10),
            Labeler = FiatLabeler,
            LabelsPaint = new SolidColorPaint(FiatLight) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(GridColor.WithAlpha(40)) { StrokeThickness = 1 },
            MinLimit = 0,
            MinZoomDelta = 12
        };

        YAxes[1] = new Axis
        {
            Name = "Bitcoin",
            NamePaint = new SolidColorPaint(BtcPrimary),
            NameTextSize = 13,
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 10),
            Labeler = BitcoinLabeler,
            LabelsPaint = new SolidColorPaint(BtcLight) { SKTypeface = SKTypeface.FromFamilyName("Inter", SKFontStyle.Normal) },
            TextSize = 12,
            SeparatorsPaint = null, // Don't duplicate grid lines
            Position = AxisPosition.End,
            MinLimit = 0,
            MinZoomDelta = 1
        };
    }

    // Series definitions with enhanced styling - stored as fields to enable disposal
    private LineSeries<ObservablePoint>? _fiatSeries;
    private LineSeries<ObservablePoint>? _btcSeries;

    private LineSeries<ObservablePoint> CreateFiatSeries() => new()
    {
        Name = "Total Wealth",
        Values = FiatValues,
        Stroke = new SolidColorPaint(FiatPrimary) { StrokeThickness = 2.5f },
        GeometryStroke = new SolidColorPaint(FiatDark) { StrokeThickness = 2 },
        GeometryFill = new SolidColorPaint(FiatLight),
        GeometrySize = 8,
        Fill = new SolidColorPaint(FiatFill),
        LineSmoothness = 0.3
    };

    private LineSeries<ObservablePoint> CreateBtcSeries() => new()
    {
        Name = "Bitcoin",
        Values = BtcValues,
        Stroke = new SolidColorPaint(BtcPrimary) { StrokeThickness = 2.5f },
        GeometryStroke = new SolidColorPaint(BtcDark) { StrokeThickness = 2 },
        GeometryFill = new SolidColorPaint(BtcLight),
        GeometrySize = 8,
        Fill = new SolidColorPaint(BtcFill),
        LineSmoothness = 0.3,
        ScalesYAt = 1
    };

    public ObservableCollection<ISeries> Series { get; } = new();

    private string BitcoinLabeler(double arg)
    {
        return CurrencyDisplay.FormatAsBitcoin((decimal)arg);
    }

    private string FiatLabeler(double arg)
    {
        return CurrencyDisplay.FormatFiat((decimal)arg, FiatCurrency?.Code ?? FiatCurrency.Usd.Code);
    }

    public void RefreshChart(WealthOverviewData wealthOverviewData)
    {
        FiatCurrency = wealthOverviewData.MainCurrency;
        FiatValues.Clear();
        BtcValues.Clear();
        PeriodLabels.Clear();

        // Reset X-axis zoom to show all data points when period changes
        XAxes[0].MinLimit = null;
        XAxes[0].MaxLimit = null;

        // Dispose old series before clearing
        DisposeSeries();
        Series.Clear();

        for (var index = 0; index < wealthOverviewData.Items.Count; index++)
        {
            var item = wealthOverviewData.Items[index];

            PeriodLabels.Add(item.Label);
            FiatValues.Add(new ObservablePoint(index, (double)item.FiatTotal));
            BtcValues.Add(new ObservablePoint(index, (double)item.BtcTotal));
        }

        // Create and add new series after data is populated
        _fiatSeries = CreateFiatSeries();
        _btcSeries = CreateBtcSeries();
        Series.Add(_fiatSeries);
        Series.Add(_btcSeries);
    }

    private void DisposeSeries()
    {
        if (_fiatSeries is not null)
        {
            (_fiatSeries.Stroke as IDisposable)?.Dispose();
            (_fiatSeries.GeometryStroke as IDisposable)?.Dispose();
            (_fiatSeries.GeometryFill as IDisposable)?.Dispose();
            (_fiatSeries.Fill as IDisposable)?.Dispose();
            _fiatSeries = null;
        }

        if (_btcSeries is not null)
        {
            (_btcSeries.Stroke as IDisposable)?.Dispose();
            (_btcSeries.GeometryStroke as IDisposable)?.Dispose();
            (_btcSeries.GeometryFill as IDisposable)?.Dispose();
            (_btcSeries.Fill as IDisposable)?.Dispose();
            _btcSeries = null;
        }
    }

    public void Dispose()
    {
        DisposeSeries();
        Series.Clear();
        FiatValues.Clear();
        BtcValues.Clear();
        PeriodLabels.Clear();
    }
}
