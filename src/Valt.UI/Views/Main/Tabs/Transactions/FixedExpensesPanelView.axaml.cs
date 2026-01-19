using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class FixedExpensesPanelView : ValtBaseUserControl
{
    public FixedExpensesPanelView()
    {
        InitializeComponent();

        FixedExpenseList.AddHandler(KeyDownEvent, FixedExpenseList_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        FixedExpenseList.AddHandler(DoubleTappedEvent, FixedExpenseList_DoubleTapped, RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        FixedExpenseList.RemoveHandler(KeyDownEvent, FixedExpenseList_KeyDown);
        FixedExpenseList.RemoveHandler(DoubleTappedEvent, FixedExpenseList_DoubleTapped);
    }

    private void FixedExpenseList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = DataContext as FixedExpensesPanelViewModel;
        if (vm is null) return;

        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<ListBox>();

        if (row is null || vm.SelectedFixedExpense is null) return;

        _ = vm.OpenFixedExpenseCommand.ExecuteAsync(null);
        e.Handled = true;
    }

    private void FixedExpenseList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var vm = DataContext as FixedExpensesPanelViewModel;
        if (vm is null || vm.SelectedFixedExpense is null) return;

        _ = vm.OpenFixedExpenseCommand.ExecuteAsync(null);
        e.Handled = true;
    }
}
