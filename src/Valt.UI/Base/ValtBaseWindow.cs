using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Valt.UI.Base;

public abstract class ValtBaseWindow : Window
{
    private IInputElement? _lastFocusedElement;

    protected ValtBaseWindow()
    {
        Activated += OnWindowActivated;
        Deactivated += OnWindowDeactivated;
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

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _lastFocusedElement = FocusManager?.GetFocusedElement();
        if (_lastFocusedElement is Window)
            _lastFocusedElement = null;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        // When a window with SystemDecorations="None" regains focus (e.g. via Alt+Tab),
        // Avalonia's internal focus state may still reference the previously focused element,
        // causing Focus() to be a no-op even though keyboard routing is disconnected.
        // ClearFocus() forces a full re-acquisition of keyboard focus.
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            FocusManager?.ClearFocus();

            if (_lastFocusedElement is Control { IsEffectivelyVisible: true, Focusable: true })
            {
                _lastFocusedElement.Focus(NavigationMethod.Tab);
            }
            else
            {
                var firstFocusable = KeyboardNavigationHandler.GetNext(this, NavigationDirection.Next);
                firstFocusable?.Focus(NavigationMethod.Tab);
            }
        }, DispatcherPriority.Input);
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