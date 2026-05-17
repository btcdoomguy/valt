using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Valt.UI.Base;

public abstract class ValtBaseWindow : Window
{
    private IInputElement? _lastFocusedElement;
    private bool _hasBeenDeactivated;

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
        // This is especially important for windows with WindowDecorations="None".
        Activate();
        var firstFocusable = global::Avalonia.Input.FocusManager.FindFirstFocusableElement(this);
        firstFocusable?.Focus();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _hasBeenDeactivated = true;
        _lastFocusedElement = FocusManager?.GetFocusedElement();
        if (_lastFocusedElement is Window)
            _lastFocusedElement = null;
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (!_hasBeenDeactivated) return;

        // When a window with WindowDecorations="None" regains focus (e.g. via Alt+Tab),
        // Avalonia's internal focus state may still reference the previously focused element,
        // causing Focus() to be a no-op even though keyboard routing is disconnected.
        // ClearFocus() forces a full re-acquisition of keyboard focus.
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            FocusManager?.Focus(null, NavigationMethod.Unspecified, KeyModifiers.None);

            if (_lastFocusedElement is Control { IsEffectivelyVisible: true, Focusable: true })
            {
                _lastFocusedElement.Focus();
            }
            else
            {
                var firstFocusable = global::Avalonia.Input.FocusManager.FindFirstFocusableElement(this);
                firstFocusable?.Focus();
            }
        }, DispatcherPriority.Render);
    }
    
    protected void CustomTitleBarButtonPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    protected void CustomTitleBarMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    protected void CustomTitleBarMaximizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    protected void CustomTitleBarCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// RAII helper that temporarily disables ExtendClientAreaToDecorationsHint on the
    /// owner window while a modal dialog is shown. This works around an Avalonia 12
    /// Windows bug where ShowDialog hangs when the parent has extended client area.
    /// </summary>
    public readonly struct ExtendClientAreaScope : IAsyncDisposable
    {
        private readonly Window? _owner;
        private readonly bool _saved;

        public ExtendClientAreaScope(Window? owner)
        {
            _owner = owner;
            if (_owner is not null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _saved = _owner.ExtendClientAreaToDecorationsHint;
                _owner.ExtendClientAreaToDecorationsHint = false;
            }
        }

        public ValueTask DisposeAsync()
        {
            if (_owner is not null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _owner.ExtendClientAreaToDecorationsHint = _saved;
            }
            return ValueTask.CompletedTask;
        }
    }
}