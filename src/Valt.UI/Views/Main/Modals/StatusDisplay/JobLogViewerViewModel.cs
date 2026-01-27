using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Valt.Infra.Kernel.BackgroundJobs;
using Valt.UI.Base;

namespace Valt.UI.Views.Main.Modals.StatusDisplay;

public partial class JobLogViewerViewModel : ValtModalViewModel
{
    private readonly IStatusItem? _statusItem;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _logContent = string.Empty;

    public JobLogViewerViewModel()
    {
        // Design-time constructor
        Title = "Job Log: Test Job";
        LogContent = "[2026-01-05 10:00:00] [Info] Job started\n[2026-01-05 10:00:01] [Info] Processing...\n[2026-01-05 10:00:02] [Info] Job completed";
        _statusItem = null;
    }

    public JobLogViewerViewModel(IStatusItem statusItem)
    {
        _statusItem = statusItem;
        Title = string.Format(Lang.language.JobLogViewer_Title, statusItem.Name);
        RefreshLogContent();
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshLogContent();
    }

    [RelayCommand]
    private void ClearLog()
    {
        _statusItem?.LogPool.Clear();
        RefreshLogContent();
    }

    private void RefreshLogContent()
    {
        if (_statusItem == null)
            return;

        var content = _statusItem.LogPool.GetAllText();
        LogContent = string.IsNullOrEmpty(content)
            ? Lang.language.JobLogViewer_NoLogs
            : content;
    }
}
