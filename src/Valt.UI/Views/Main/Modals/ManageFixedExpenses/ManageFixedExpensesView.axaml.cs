using Avalonia.Input;
using Avalonia.Interactivity;
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
        (DataContext as ManageFixedExpensesViewModel).EditFixedExpenseCommand.Execute(null);
    }
}