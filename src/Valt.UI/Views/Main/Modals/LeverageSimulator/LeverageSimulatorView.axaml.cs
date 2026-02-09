using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.LeverageSimulator;

public partial class LeverageSimulatorView : ValtBaseWindow
{
    public LeverageSimulatorView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
