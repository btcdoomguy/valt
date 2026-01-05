using Avalonia.Input;
using Valt.Infra.Kernel.BackgroundJobs;
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
        if (DataContext is StatusDisplayViewModel vm && vm.SelectedJob != null)
        {
            vm.OpenJobLogCommand.Execute(vm.SelectedJob);
        }
    }
}