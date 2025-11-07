using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.IconSelector;

public partial class IconSelectorView : ValtBaseWindow
{
    public IconSelectorView()
    {
        InitializeComponent();

        IconColorPicker.SelectedIndex = (int)ColorViewTab.Palette;
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