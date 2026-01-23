using Avalonia.Controls;
using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.ManageFixedExpenses;

public partial class ManageFixedExpensesView : ValtBaseWindow
{
    public ManageFixedExpensesView()
    {
        InitializeComponent();
    }

    private void MainGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as ManageFixedExpensesViewModel)?.EditFixedExpenseCommand.Execute(null);
    }

    private void MainGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ManageFixedExpensesViewModel vm && sender is DataGrid dataGrid)
        {
            vm.SelectedFixedExpense = dataGrid.SelectedItem as ManageFixedExpensesViewModel.FixedExpenseListItemViewModel;
        }
    }
}