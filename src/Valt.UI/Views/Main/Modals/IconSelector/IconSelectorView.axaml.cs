using Avalonia.Controls;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.IconSelector;

public partial class IconSelectorView : ValtBaseWindow
{
    public IconSelectorView()
    {
        InitializeComponent();

        IconColorPicker.SelectedIndex = (int)ColorViewTab.Palette;
    }
}