using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Valt.UI.Base;

public abstract class ValtBaseUserControl : UserControl
{
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is not ValtViewModel vm) return;

        vm.GetUserControlOwnerWindow = () => this.FindAncestorOfType<Window>();
    }
}