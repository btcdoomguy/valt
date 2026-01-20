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
        e.Cancel = true;

        SaveWindowSettings();

        if (DataContext is not MainViewModel viewModel)
            return;

        await viewModel.OnClosingAsync();

        e.Cancel = false;
    }
}