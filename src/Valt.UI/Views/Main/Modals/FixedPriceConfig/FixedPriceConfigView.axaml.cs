using Avalonia.Controls;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.FixedPriceConfig;

public partial class FixedPriceConfigView : ValtBaseWindow
{
    public FixedPriceConfigView()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            PriceTextBox.Focus();
            PriceTextBox.SelectAll();
        }, DispatcherPriority.Render);
    }
}