using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.BtcLoanSimulator;

public partial class BtcLoanSimulatorView : ValtBaseWindow
{
    public BtcLoanSimulatorView()
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
