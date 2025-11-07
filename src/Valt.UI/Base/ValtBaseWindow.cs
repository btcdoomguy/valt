using System;
using Avalonia.Controls;

namespace Valt.UI.Base;

public abstract class ValtBaseWindow : Window
{
    protected override void OnOpened(EventArgs e)
    {
        if (DataContext is not IValtModal vm) return;

        vm.CloseWindow = Close;
        vm.CloseDialog = (param) => Close(param);
        vm.GetWindow = () => this;
    }
}