using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView.Avalonia;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class ReportsView : ValtBaseUserControl
{
    CartesianChart? _wealthOverviewChart;
    CartesianChart? _categoryBarChart;
    CartesianChart? _incomeCategoryBarChart;
    private ReportsViewModel? _viewModel;

    public ReportsView()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _categoryBarChart = CategoryBarChart;
        _wealthOverviewChart = WealthOverviewChart;
        _incomeCategoryBarChart = IncomeCategoryBarChart;

        // Allow scroll events to pass through charts to the parent ScrollViewer
        _wealthOverviewChart?.AddHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _categoryBarChart?.AddHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
        _incomeCategoryBarChart?.AddHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);

        if (DataContext is ReportsViewModel vm)
        {
            _viewModel = vm;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            _viewModel.Initialize();

            Dispatcher.UIThread.Post(() => ForceWealthOverviewRedraw(), DispatcherPriority.Background);
        }
    }

    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Reset Handled to false so scroll events bubble up to the parent ScrollViewer
        e.Handled = false;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _viewModel = null;
        }

        _wealthOverviewChart?.RemoveHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged);
        _categoryBarChart?.RemoveHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged);
        _incomeCategoryBarChart?.RemoveHandler(PointerWheelChangedEvent, OnChartPointerWheelChanged);

        _wealthOverviewChart = null;
        _categoryBarChart = null;
        _incomeCategoryBarChart = null;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReportsViewModel.IsWealthOverviewLoading))
        {
            ForceWealthOverviewRedraw();
        }
    }

    private void ForceWealthOverviewRedraw()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _wealthOverviewChart?.InvalidateMeasure();
            _wealthOverviewChart?.InvalidateVisual();
            _wealthOverviewChart?.CoreChart.Update(new ChartUpdateParams()
            {
                IsAutomaticUpdate = true,
                Throttling = false
            });
        });
    }

    private void ForceCategoryBarRedraw()
    {
        _categoryBarChart?.InvalidateMeasure();
        _categoryBarChart?.InvalidateVisual();
        _categoryBarChart?.CoreChart.Update(new ChartUpdateParams()
        {
            IsAutomaticUpdate = true,
            Throttling = false
        });
    }
}