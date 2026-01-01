using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsView : ValtBaseUserControl
{
    public TransactionsView()
    {
        InitializeComponent();

        FixedExpenseList.AddHandler(KeyDownEvent, FixedExpenseList_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        FixedExpenseList.AddHandler(DoubleTappedEvent, FixedExpenseList_DoubleTapped, RoutingStrategies.Bubble,
            handledEventsToo: true);
        AccountsList.AddHandler(KeyDownEvent, AccountsList_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        FixedExpenseList.RemoveHandler(KeyDownEvent, FixedExpenseList_KeyDown);
        FixedExpenseList.RemoveHandler(DoubleTappedEvent, FixedExpenseList_DoubleTapped);
        AccountsList.RemoveHandler(KeyDownEvent, AccountsList_KeyDown);
    }

    private void FixedExpenseList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = (DataContext as TransactionsViewModel)!;
        
        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<ListBox>();

        if (row is null || vm.SelectedFixedExpense is null) return;
        
        _ = vm.OpenFixedExpenseCommand.ExecuteAsync(null);
        e.Handled = true;
    }

    private void FixedExpenseList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        var vm = (DataContext as TransactionsViewModel)!;
        if (vm.SelectedFixedExpense is null) return;

        _ = vm.OpenFixedExpenseCommand.ExecuteAsync(null);
        e.Handled = true;
    }

    private async void AccountsList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Shift) return;

        var vm = (DataContext as TransactionsViewModel)!;
        if (vm.SelectedAccount is null) return;

        if (e.Key == Key.Up)
        {
            await vm.MoveUpAccountCommand.ExecuteAsync(vm.SelectedAccount);
            e.Handled = true;
            Dispatcher.UIThread.Post(() => AccountsList.Focus());
        }
        else if (e.Key == Key.Down)
        {
            await vm.MoveDownAccountCommand.ExecuteAsync(vm.SelectedAccount);
            e.Handled = true;
            Dispatcher.UIThread.Post(() => AccountsList.Focus());
        }
    }
}