using Avalonia.Input;
using Avalonia.Interactivity;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.Settings;

public partial class SettingsView : ValtBaseWindow
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void CustomTitleBar_OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}