using Avalonia.Interactivity;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.StatusDisplay;

public partial class JobLogViewerView : ValtBaseWindow
{
    public JobLogViewerView()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
