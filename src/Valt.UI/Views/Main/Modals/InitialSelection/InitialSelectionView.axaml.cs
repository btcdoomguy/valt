using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.InitialSelection;

public partial class InitialSelectionView : ValtBaseWindow
{
    public InitialSelectionView()
    {
        InitializeComponent();
        
        AddHandler(KeyDownEvent, RecentFilesList_OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: false);
    }

    protected override void OnOpened(EventArgs e)
    {
        if (!Design.IsDesignMode && DataContext is not null)
            ((DataContext as InitialSelectionViewModel)!).LoadRecentFilesCommand.Execute(null);

        base.OnOpened(e);

        Dispatcher.UIThread.InvokeAsync(() => FileBox.Focus(), DispatcherPriority.ApplicationIdle);
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount > 1)
        {
            ((DataContext as InitialSelectionViewModel)!).OpenSelectedCommand.Execute(null);
        }
    } 

    private void RecentFilesList_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is null) return;
        
        ((DataContext as InitialSelectionViewModel)!).OpenSelectedCommand.Execute(null);
        e.Handled = true;
        return;
    }
}