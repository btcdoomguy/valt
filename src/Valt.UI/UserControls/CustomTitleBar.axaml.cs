using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Valt.UI.UserControls;

public partial class CustomTitleBar : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CustomTitleBar, string>(nameof(Title));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public event EventHandler<PointerPressedEventArgs>? TitleBarPressed;

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        TitleBarPressed?.Invoke(this, e);
    }
    
    public event EventHandler<RoutedEventArgs>? MinimizeClick;
    
    private void Minimize_OnClick(object? sender, RoutedEventArgs e)
    {
        MinimizeClick?.Invoke(this, e);
    }
    
    public event EventHandler<RoutedEventArgs>? MaximizeClick;

    private void Maximize_OnClick(object? sender, RoutedEventArgs e)
    {
        MaximizeClick?.Invoke(this, e);
    }
    
    public event EventHandler<RoutedEventArgs>? CloseClick;

    private void Close_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseClick?.Invoke(this, e);
    }
    
    public static readonly StyledProperty<bool> MinimizeEnabledProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(MinimizeEnabled), false);

    public bool MinimizeEnabled
    {
        get => GetValue(MinimizeEnabledProperty);
        set => SetValue(MinimizeEnabledProperty, value);
    }

    public static readonly StyledProperty<bool> MaximizeEnabledProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(MaximizeEnabled), false);

    public bool MaximizeEnabled
    {
        get => GetValue(MaximizeEnabledProperty);
        set => SetValue(MaximizeEnabledProperty, value);
    }

    public static readonly StyledProperty<bool> CloseEnabledProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(CloseEnabled), true);

    public bool CloseEnabled
    {
        get => GetValue(CloseEnabledProperty);
        set => SetValue(CloseEnabledProperty, value);
    }

    public static readonly StyledProperty<bool> MinimizeVisibleProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(MinimizeVisible), false);

    public bool MinimizeVisible
    {
        get => GetValue(MinimizeVisibleProperty);
        set => SetValue(MinimizeVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> MaximizeVisibleProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(MaximizeVisible), false);

    public bool MaximizeVisible
    {
        get => GetValue(MaximizeVisibleProperty);
        set => SetValue(MaximizeVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> CloseVisibleProperty =
        AvaloniaProperty.Register<CustomTitleBar, bool>(nameof(CloseVisible), true);

    public bool CloseVisible
    {
        get => GetValue(CloseVisibleProperty);
        set => SetValue(CloseVisibleProperty, value);
    }

    public CustomTitleBar()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}