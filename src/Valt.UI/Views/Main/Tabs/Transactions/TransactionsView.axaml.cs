using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionsView : ValtBaseUserControl
{
    private ILocalStorageService? _localStorageService;

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
            _localStorageService = App.ServiceProvider.GetRequiredService<ILocalStorageService>();
        }

        RestoreLayoutSettings();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        SaveLayoutSettings();
        AccountsList.RemoveHandler(KeyDownEvent, AccountsList_KeyDown);
    }

    private void RestoreLayoutSettings()
    {
        if (_localStorageService is null) return;

        var settings = _localStorageService.LoadLayoutSettings();

        // Right panel column is at index 3
        if (settings.RightPanelWidth.HasValue && MainLayoutGrid.ColumnDefinitions.Count > 3)
        {
            MainLayoutGrid.ColumnDefinitions[3].Width = new GridLength(settings.RightPanelWidth.Value, GridUnitType.Pixel);
        }

        // Fixed expenses panel row is at index 1 in RightPanelGrid
        if (settings.FixedExpensesPanelHeight.HasValue && RightPanelGrid.RowDefinitions.Count > 1)
        {
            RightPanelGrid.RowDefinitions[1].Height = new GridLength(settings.FixedExpensesPanelHeight.Value, GridUnitType.Pixel);
        }
    }

    private void SaveLayoutSettings()
    {
        if (_localStorageService is null) return;

        var rightPanelWidth = MainLayoutGrid.ColumnDefinitions.Count > 3
            ? MainLayoutGrid.ColumnDefinitions[3].ActualWidth
            : 270;

        var fixedExpensesPanelHeight = RightPanelGrid.RowDefinitions.Count > 1
            ? RightPanelGrid.RowDefinitions[1].ActualHeight
            : 0;

        var settings = new LayoutSettings
        {
            RightPanelWidth = rightPanelWidth,
            FixedExpensesPanelHeight = fixedExpensesPanelHeight
        };

        _ = _localStorageService.SaveLayoutSettingsAsync(settings);
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
