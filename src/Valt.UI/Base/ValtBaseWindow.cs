using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Valt.UI.Base;

public abstract class ValtBaseWindow : Window
{
    protected ValtBaseWindow()
    {
        Activated += OnWindowActivated;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is IValtModal vm)
        {
            vm.CloseWindow = Close;
            vm.CloseDialog = (param) => Close(param);
            vm.GetWindow = () => this;
        }

        // Ensure the modal window takes focus when opened.
        // This is especially important for windows with SystemDecorations="None".
        Activate();
        var firstFocusable = KeyboardNavigationHandler.GetNext(this, NavigationDirection.Next);
        firstFocusable?.Focus();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        // When a window with SystemDecorations="None" regains focus,
        // keyboard focus doesn't automatically go to a child control.
        // This ensures the first focusable element receives keyboard focus.
        if (FocusManager?.GetFocusedElement() is null or Window)
        {
            var firstFocusable = KeyboardNavigationHandler.GetNext(this, NavigationDirection.Next);
            firstFocusable?.Focus();
        }
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