using System;
using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Valt.App.Modules.SpendingAnalytics.DTOs;
using Valt.Core.Common;
using Valt.Infra.Kernel;
using Valt.UI.Lang;

namespace Valt.UI.Views.Main.Tabs.Reports;

public class FixedVsVariableChartData : ReportChartDataBase
{
    private static readonly SKColor FixedColor = SKColor.Parse("#0566e9");       // Secondary500
    private static readonly SKColor VariableColor = SKColor.Parse("#ffa122");    // Accent400

    public string PrimaryCurrency { get; set; } = FiatCurrency.Usd.Code;

    public ObservableCollection<double> FixedValues { get; } = new();
    public ObservableCollection<double> VariableValues { get; } = new();

    private StackedColumnSeries<double>? _fixedSeries;
    private StackedColumnSeries<double>? _variableSeries;

    public FixedVsVariableChartData()
    {
        YAxes[0] = CreateValueAxis(FiatLabeler, minLimit: 0);
    }

    private string FiatLabeler(double value)
    {
        return CurrencyDisplay.FormatFiat((decimal)value, PrimaryCurrency);
    }

    private StackedColumnSeries<double> CreateFixedSeries() => new()
    {
        Name = language.Reports_FixedVariable_Fixed,
        Values = FixedValues,
        Stroke = null,
        Fill = new SolidColorPaint(FixedColor)
    };

    private StackedColumnSeries<double> CreateVariableSeries() => new()
    {
        Name = language.Reports_FixedVariable_Variable,
        Values = VariableValues,
        Stroke = null,
        Fill = new SolidColorPaint(VariableColor)
    };

    public void RefreshChart(FixedVsVariableDataDto data)
    {
        PrimaryCurrency = data.PrimaryCurrency;

        FixedValues.Clear();
        VariableValues.Clear();
        MonthLabels.Clear();

        BeginSeriesRebuild();

        foreach (var month in data.Months)
        {
            MonthLabels.Add(month.Month.ToString("MMM yyyy", CultureInfo.InvariantCulture));
            FixedValues.Add((double)month.FixedTotal);
            VariableValues.Add((double)month.VariableTotal);
        }

        _fixedSeries = CreateFixedSeries();
        _variableSeries = CreateVariableSeries();

        Series.Add(_fixedSeries);
        Series.Add(_variableSeries);
    }

    protected override void DisposeSeries()
    {
        DisposePaints(_fixedSeries?.Fill);
        _fixedSeries = null;

        DisposePaints(_variableSeries?.Fill);
        _variableSeries = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        FixedValues.Clear();
        VariableValues.Clear();
    }
}
