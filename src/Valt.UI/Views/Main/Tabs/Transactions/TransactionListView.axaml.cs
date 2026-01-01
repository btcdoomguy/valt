using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionListView : ValtBaseUserControl
{
    private bool _ignoreFirstOnSorting = false;

    public TransactionListView()
    {
        InitializeComponent();

        MainGrid.AddHandler(KeyDownEvent, MainGrid_KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
        MainGrid.AddHandler(DoubleTappedEvent, MainGrid_OnDoubleTapped, RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var viewModel = DataContext as TransactionListViewModel;

        SearchBox.AsyncPopulator = viewModel!.GetSearchTermsAsync;

        RestoreDataGridSettings();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        SaveDataGridSettings();

        MainGrid.RemoveHandler(KeyDownEvent, MainGrid_KeyDownHandler);
        MainGrid.RemoveHandler(DoubleTappedEvent, MainGrid_OnDoubleTapped);
        MainGrid.Sorting -= MainGrid_OnSorting;
    }

    private void RestoreDataGridSettings()
    {
        var vm = DataContext as TransactionListViewModel;
        if (vm is null) return;

        var settings = vm.GetDataGridSettings();

        if (settings.ColumnOrder.Count > 0)
        {
            for (int i = 0; i < settings.ColumnOrder.Count; i++)
            {
                var header = settings.ColumnOrder[i];
                var column = MainGrid.Columns.FirstOrDefault(c => c.Tag?.ToString() == header);
                if (column != null)
                {
                    column.DisplayIndex = i;
                }
            }
        }

        foreach (var column in MainGrid.Columns)
        {
            var columnId = column.Tag?.ToString();
            if (!string.IsNullOrEmpty(columnId) && settings.ColumnWidths.TryGetValue(columnId, out var width))
            {
                column.Width = new DataGridLength(width, DataGridLengthUnitType.Pixel);
            }

            if (columnId == settings.OrderedColumn)
            {
                _ignoreFirstOnSorting = true;
                column.Sort(settings.SortDirection.GetValueOrDefault(ListSortDirection.Ascending));
            }
        }

        MainGrid.Sorting += MainGrid_OnSorting;
    }

    private void SaveDataGridSettings()
    {
        var vm = DataContext as TransactionListViewModel;
        if (vm is null) return;

        var columns = MainGrid.Columns.Select(c => new DataGridColumnInfo
        {
            Tag = c.Tag?.ToString() ?? string.Empty,
            Width = c.Width.DisplayValue,
            DisplayIndex = c.DisplayIndex
        });

        vm.SaveDataGridSettings(columns);
    }

    private void MainGrid_OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (_ignoreFirstOnSorting)
        {
            _ignoreFirstOnSorting = false;
            return;
        }

        var vm = DataContext as TransactionListViewModel;
        vm?.UpdateSortState(e.Column.Header?.ToString());
    }

    private void MainGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var vm = (DataContext as TransactionListViewModel)!;

        var originalSource = e.Source as Control;
        var row = originalSource?.FindAncestorOfType<DataGridRow>();

        if (row is null || vm.SelectedTransaction is null) return;

        _ = vm.EditTransactionCommand.ExecuteAsync(vm.SelectedTransaction);
        e.Handled = true;
    }

    private void MainGrid_KeyDownHandler(object? sender, KeyEventArgs e)
    {
        var vm = (DataContext as TransactionListViewModel)!;

        if (e.Key == Key.C && e.KeyModifiers == KeyModifiers.Control)
        {
            if (vm.SelectedTransaction is null || !vm.IsSingleItemSelected) return;

            _ = vm.CopyTransactionCommand.ExecuteAsync(vm.SelectedTransaction);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter) return;

        if (vm.SelectedTransaction is null) return;

        _ = vm.EditTransactionCommand.ExecuteAsync(vm.SelectedTransaction);
        e.Handled = true;
    }

    private void MainGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is TransactionListViewModel viewModel && sender is DataGrid dataGrid)
        {
            viewModel.UpdateSelectedItems(dataGrid.SelectedItems);
        }
    }

    private void SearchBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is TransactionListViewModel viewModel)
        {
            viewModel.ApplySearchCommand.Execute(null);
        }
    }

    private void SearchBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (DataContext is TransactionListViewModel viewModel)
        {
            viewModel.ApplySearchCommand.Execute(null);
        }
    }
}
