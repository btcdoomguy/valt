using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;


namespace Valt.UI.Views.Main;

public partial class MainView : ValtBaseWindow
{
    private ILocalStorageService? _localStorageService;
    private bool _isClosing;

    public MainView()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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

        if (App.ServiceProvider != null)
        {
            _localStorageService = App.ServiceProvider.GetRequiredService<ILocalStorageService>();
        }

        RestoreWindowSettings();

        ((DataContext as MainViewModel)!).Window = this;
        _ = ((DataContext as MainViewModel)!).OpenInitialSelectionModal();
    }

    private void RestoreWindowSettings()
    {
        if (_localStorageService is null) return;

        var settings = _localStorageService.LoadWindowSettings();

        if (settings.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveWindowSettings()
    {
        if (_localStorageService is null) return;

        var settings = new WindowSettings
        {
            IsMaximized = WindowState == WindowState.Maximized
        };

        _ = _localStorageService.SaveWindowSettingsAsync(settings);
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
        // If we're already in the closing process, allow the close to proceed
        if (_isClosing)
            return;

        // Cancel this close attempt - we'll close again after async cleanup
        e.Cancel = true;
        _isClosing = true;

        SaveWindowSettings();

        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.OnClosingAsync();
        }

        // Now close the window for real
        Close();
    }
}