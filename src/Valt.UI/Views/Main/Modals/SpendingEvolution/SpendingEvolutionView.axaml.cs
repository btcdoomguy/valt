using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.SpendingEvolution;

public partial class SpendingEvolutionView : ValtBaseWindow
{
    public SpendingEvolutionView()
    {
        InitializeComponent();
    }

    private void OnFilterSettingsButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu is not null)
        {
            button.ContextMenu.Placement = PlacementMode.BottomEdgeAlignedRight;
            button.ContextMenu.Open();
        }
    }
}
