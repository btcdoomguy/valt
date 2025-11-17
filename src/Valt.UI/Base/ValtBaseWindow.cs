using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

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
    
    protected void CustomTitleBarButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    protected void CustomTitleBarCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}