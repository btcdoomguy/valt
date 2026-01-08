using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.FixedExpenseHistory;

public partial class FixedExpenseHistoryView : ValtBaseWindow
{
    public FixedExpenseHistoryView()
    {
        InitializeComponent();
    }

    private void TransactionsGrid_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        (DataContext as FixedExpenseHistoryViewModel)?.EditTransactionCommand.Execute(null);
    }
}
