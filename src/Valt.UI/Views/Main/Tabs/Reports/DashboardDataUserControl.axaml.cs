using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Valt.UI.Views.Main.Tabs.Reports;

public partial class DashboardDataUserControl : UserControl
{
    public DashboardDataUserControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DashboardData data)
        {
            UpdateStaleState(data.IsStale);
        }
    }

    private void UpdateStaleState(bool isStale)
    {
        if (isStale)
            Classes.Add("stale");
        else
            Classes.Remove("stale");
    }

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border { DataContext: RowItem { HasUrl: true, Url: { } url } })
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently ignore if browser can't be opened
        }
    }
}
