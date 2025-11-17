using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;
using Valt.UI.Base;


namespace Valt.UI.Views.Main;

public partial class MainView : ValtBaseWindow
{
    private WindowState _windowState = WindowState.Normal;

    public MainView()
    {
        InitializeComponent();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaTitleBarHeightHint = -1;
            SystemDecorations = SystemDecorations.Full;
            CustomTitleBar.IsVisible = false;
            AlternateTitleBar.IsVisible = true;
        }
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (Design.IsDesignMode) 
            return;
        
        ((DataContext as MainViewModel)!).Window = this;
        _ = ((DataContext as MainViewModel)!).OpenInitialSelectionModal();
    }

    private void Window_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (DataContext is not MainViewModel viewModel) 
            return;
        
        await viewModel.OnClosingAsync();

        e.Cancel = false;
    }
}