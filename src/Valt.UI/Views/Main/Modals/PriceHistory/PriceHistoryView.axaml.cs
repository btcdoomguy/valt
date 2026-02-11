using System;
using Avalonia.Input;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.PriceHistory;

public partial class PriceHistoryView : ValtBaseWindow
{
    public PriceHistoryView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is PriceHistoryViewModel vm)
        {
            vm.Initialize();
        }
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
