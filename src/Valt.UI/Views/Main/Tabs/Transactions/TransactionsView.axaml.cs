using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsView : ValtBaseUserControl
{
    public TransactionsView()
    {
        InitializeComponent();

        AccountsList.AddHandler(KeyDownEvent, AccountsList_KeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Design.IsDesignMode)
            return;

        // Get ViewModels from DI and set DataContext for panels
        if (App.ServiceProvider != null)
        {
            FixedExpensesPanel.DataContext = App.ServiceProvider.GetRequiredService<FixedExpensesPanelViewModel>();
            GoalsPanel.DataContext = App.ServiceProvider.GetRequiredService<GoalsPanelViewModel>();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        AccountsList.RemoveHandler(KeyDownEvent, AccountsList_KeyDown);
    }

    private async void AccountsList_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Control) return;

        var vm = DataContext as TransactionsViewModel;
        if (vm?.SelectedAccount is null) return;

        if (e.Key == Key.Up)
        {
            await vm.MoveUpAccountCommand.ExecuteAsync(vm.SelectedAccount);
            e.Handled = true;
            FocusSelectedAccountItem();
        }
        else if (e.Key == Key.Down)
        {
            await vm.MoveDownAccountCommand.ExecuteAsync(vm.SelectedAccount);
            e.Handled = true;
            FocusSelectedAccountItem();
        }
    }

    private void FocusSelectedAccountItem()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (AccountsList.SelectedItem is null) return;

            var index = AccountsList.SelectedIndex;
            if (index >= 0)
            {
                AccountsList.ContainerFromIndex(index)?.Focus();
            }
        }, DispatcherPriority.Background);
    }
}
