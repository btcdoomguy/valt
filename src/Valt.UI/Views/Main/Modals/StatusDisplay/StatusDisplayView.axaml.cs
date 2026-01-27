using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.StatusDisplay;

public partial class StatusDisplayView : ValtBaseWindow
{
    public StatusDisplayView()
    {
        InitializeComponent();
    }

    private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is StatusDisplayViewModel vm && vm.SelectedItem != null)
        {
            vm.OpenItemLogCommand.Execute(vm.SelectedItem);
        }
    }
}