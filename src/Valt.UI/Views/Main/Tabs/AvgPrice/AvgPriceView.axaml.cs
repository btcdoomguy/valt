using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.AvgPrice;

public partial class AvgPriceView : ValtBaseUserControl
{
    public AvgPriceView()
    {
        InitializeComponent();

        MainGrid.AddHandler(KeyDownEvent, MainGrid_KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
        MainGrid.AddHandler(DoubleTappedEvent, MainGrid_OnDoubleTapped, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (DataContext is AvgPriceViewModel vm)
        {
            vm.Initialize();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        MainGrid.RemoveHandler(KeyDownEvent, MainGrid_KeyDownHandler);
        MainGrid.RemoveHandler(DoubleTappedEvent, MainGrid_OnDoubleTapped);
    }

    private void MainGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = DataContext as AvgPriceViewModel;
        if (vm is null) return;

        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<DataGridRow>();

        if (row is null || vm.SelectedLine is null) return;

        _ = vm.EditOperationCommand.ExecuteAsync(null);
        e.Handled = true;
    }

    private void MainGrid_KeyDownHandler(object? sender, KeyEventArgs e)
    {
        var vm = DataContext as AvgPriceViewModel;
        if (vm is null) return;

        // F2 to add new operation
        if (e.Key == Key.F2)
        {
            _ = vm.AddOperationCommand.ExecuteAsync(null);
            e.Handled = true;
            return;
        }

        // Enter to edit selected operation
        if (e.Key == Key.Enter && vm.SelectedLine is not null)
        {
            _ = vm.EditOperationCommand.ExecuteAsync(null);
            e.Handled = true;
        }
    }
}