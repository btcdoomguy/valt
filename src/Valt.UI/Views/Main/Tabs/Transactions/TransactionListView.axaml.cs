using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class TransactionListView : ValtBaseUserControl
{
    private string? _orderedColumn;
    private ListSortDirection? _sortDirection;
    private bool _ignoreFirstOnSorting = false;

    public TransactionListView()
    {
        InitializeComponent();
        
        Loaded += (s, e) => RestoreDataGridSettings();
        Unloaded += (s, e) => LocalStorageHelper.ChangeDataGridSettings(MainGrid, _orderedColumn, _sortDirection);

        MainGrid.AddHandler(KeyDownEvent, MainGrid_KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
        MainGrid.AddHandler(DoubleTappedEvent, MainGrid_OnDoubleTapped, RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        var viewModel = DataContext as TransactionListViewModel;

        SearchBox.AsyncPopulator = viewModel!.GetSearchTermsAsync;
    }

    private void RestoreDataGridSettings()
    {
        var settings = LocalStorageHelper.LoadDataGridSettings();

        if (settings.ColumnOrder.Count > 0)
        {
            for (int i = 0; i < settings.ColumnOrder.Count; i++)
            {
                var header = settings.ColumnOrder[i];
                var column = MainGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);
                if (column != null)
                {
                    column.DisplayIndex = i;
                }
            }
        }

        foreach (var column in MainGrid.Columns)
        {
            var columnId = column.Header?.ToString();
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

        _orderedColumn = settings.OrderedColumn;
        _sortDirection = settings.SortDirection;

        MainGrid.Sorting += MainGrid_OnSorting;
    }
    
    private void MainGrid_OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (_ignoreFirstOnSorting)
        {
            _ignoreFirstOnSorting = false;
            return;
        }

        var columnId = e.Column.Header?.ToString();

        //this is crappy but avalonia doesn't make life easy to indicate which sort direction was selected
        if (_orderedColumn == columnId)
        {
            _sortDirection = _sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }
        else
        {
            _orderedColumn = columnId;
            _sortDirection = ListSortDirection.Ascending;
        }
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
        if (e.Key != Key.Enter) return;
        
        var vm = (DataContext as TransactionListViewModel)!;
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