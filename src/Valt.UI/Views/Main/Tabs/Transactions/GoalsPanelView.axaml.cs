using Avalonia.Controls;
using Valt.UI.Base;
using Valt.UI.Views.Main.Tabs.Transactions.Models;

namespace Valt.UI.Views.Main.Tabs.Transactions;

public partial class GoalsPanelView : ValtBaseUserControl
{
    public GoalsPanelView()
    {
        InitializeComponent();
    }

    private void GoalsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is GoalsPanelViewModel vm && sender is ListBox listBox)
        {
            vm.SelectedGoal = listBox.SelectedItem as GoalEntryViewModel;
        }
    }
}
