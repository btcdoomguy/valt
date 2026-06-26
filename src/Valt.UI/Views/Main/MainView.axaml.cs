using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Valt.UI.Base;
using Valt.UI.Services.LocalStorage;


namespace Valt.UI.Views.Main;

public partial class MainView : ValtBaseWindow
{
    private ILocalStorageService? _localStorageService;
    private IFireAndForgetTaskRunner? _runner;
    private ILogger<MainView>? _logger;

    public MainView()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaTitleBarHeightHint = -1;
            WindowDecorations = WindowDecorations.Full;
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
            _runner = App.ServiceProvider.GetRequiredService<IFireAndForgetTaskRunner>();
            _logger = App.ServiceProvider.GetRequiredService<ILogger<MainView>>();
        }

        RestoreWindowSettings();

        ((DataContext as MainViewModel)!).Window = this;
        _ = Dispatcher.UIThread.InvokeAsync(async () => await StartupSequenceAsync(), DispatcherPriority.ApplicationIdle);
    }

    private async System.Threading.Tasks.Task StartupSequenceAsync()
    {
        var vm = (DataContext as MainViewModel)!;
        await vm.OpenInitialSelectionModal();
        await vm.OpenTipsModalIfNeededAsync();
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
        if (_localStorageService is null || _runner is null || _logger is null) return;

        var settings = new WindowSettings
        {
            IsMaximized = WindowState == WindowState.Maximized
        };

        _runner.RunAsync(_localStorageService.SaveWindowSettingsAsync(settings), _logger);
    }

    private void Window_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Cleanup moved to application lifetime ShutdownRequested.
        // This handler is now synchronous and only saves synchronous state.
        SaveWindowSettings();
    }
}