using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsView : ValtBaseUserControl
{
    CartesianChart? _monthlyTotalsChart;
    PieChart? _categoryPieChart;
    
    public ReportsView()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        _categoryPieChart = CategoryPieChart;
        _monthlyTotalsChart = MonthlyChart;
        
        if (DataContext is ReportsViewModel vm)
        {
            vm.PropertyChanged += ViewModelOnPropertyChanged;
            vm.Initialize();
            
            Dispatcher.UIThread.Post(() => ForceMonthlyTotalsRedraw(), DispatcherPriority.Background);
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReportsViewModel.IsMonthlyTotalsLoading))
        {
            ForceMonthlyTotalsRedraw();
        }
    }
    
    private void ForceMonthlyTotalsRedraw()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _monthlyTotalsChart?.InvalidateMeasure();
            _monthlyTotalsChart?.InvalidateVisual();
            _monthlyTotalsChart?.CoreChart.Update(new ChartUpdateParams()
            {
                IsAutomaticUpdate = true,
                Throttling = false
            });
        });
    }

    private void ForceCategoryPieRedraw()
    {
        _categoryPieChart?.InvalidateMeasure();
        _categoryPieChart?.InvalidateVisual();
        _categoryPieChart?.CoreChart.Update(new ChartUpdateParams()
        {
            IsAutomaticUpdate = true,
            Throttling = false
        });
    }
}