using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Valt.Infra.Services.Updates;
using Valt.UI.Lang;

namespace Valt.UI.UserControls;

public partial class UpdateIndicatorViewModel : ObservableObject
{
    private readonly IUpdateChecker _updateChecker;
    private readonly ILogger<UpdateIndicatorViewModel> _logger;

    private UpdateInfo? _updateInfo;
    private Window? _ownerWindow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateText))]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateText))]
    private string _newVersion = string.Empty;

    [ObservableProperty]
    private string _releaseNotes = string.Empty;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private bool _canDownload;

    [ObservableProperty]
    private string _downloadError = string.Empty;

    public string UpdateText => string.Format(language.Update_Available, NewVersion);

    /// <summary>
    /// Design-time constructor
    /// </summary>
    public UpdateIndicatorViewModel()
    {
        _updateChecker = null!;
        _logger = null!;

        // Design-time data
        IsUpdateAvailable = true;
        NewVersion = "v0.2.0";
        ReleaseNotes = "## What's New\n\n- Feature 1\n- Feature 2\n- Bug fixes";
        CanDownload = true;
    }

    public UpdateIndicatorViewModel(IUpdateChecker updateChecker, ILogger<UpdateIndicatorViewModel> logger)
    {
        _updateChecker = updateChecker;
        _logger = logger;
    }

    public void SetOwnerWindow(Window window)
    {
        _ownerWindow = window;
    }

    public async Task CheckForUpdateAsync()
    {
        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            if (currentVersion is null)
            {
                _logger.LogWarning("Could not determine current assembly version");
                return;
            }

            _updateInfo = await _updateChecker.CheckForUpdateAsync(currentVersion);

            if (_updateInfo is not null)
            {
                NewVersion = _updateInfo.Version;
                ReleaseNotes = _updateInfo.ReleaseNotes;
                CanDownload = FindAssetForPlatform() is not null;
                IsUpdateAvailable = true;

                _logger.LogInformation("Update available: {Version}", _updateInfo.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
        }
    }

    [RelayCommand]
    private async Task DownloadUpdate()
    {
        if (_updateInfo is null || _ownerWindow is null)
            return;

        var asset = FindAssetForPlatform();
        if (asset is null)
        {
            DownloadError = language.Update_NoPlatformAsset;
            return;
        }

        try
        {
            IsDownloading = true;
            DownloadError = string.Empty;
            DownloadProgress = 0;

            // Show save file dialog
            var suggestedFileName = asset.Name;
            var saveOptions = new FilePickerSaveOptions
            {
                Title = language.Update_SaveDialogTitle,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "zip",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("ZIP Files") { Patterns = new[] { "*.zip" } }
                }
            };

            var file = await _ownerWindow.StorageProvider.SaveFilePickerAsync(saveOptions);
            if (file is null)
            {
                IsDownloading = false;
                return;
            }

            var filePath = file.Path.LocalPath;

            // Download the file
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            using var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
            var buffer = new byte[8192];
            var bytesRead = 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            int read;
            while ((read = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (totalBytes > 0)
                {
                    DownloadProgress = (int)((bytesRead * 100) / totalBytes);
                }
            }

            _logger.LogInformation("Update downloaded successfully to {Path}", filePath);

            // Open the containing folder
            OpenContainingFolder(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update");
            DownloadError = language.Update_DownloadError;
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 0;
        }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        if (_updateInfo is null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _updateInfo.HtmlUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening GitHub URL");
        }
    }

    private ReleaseAsset? FindAssetForPlatform()
    {
        if (_updateInfo is null)
            return null;

        var platform = GetPlatformIdentifier();
        return _updateInfo.Assets.FirstOrDefault(a =>
            a.Name.Contains(platform, StringComparison.OrdinalIgnoreCase) &&
            a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetPlatformIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        return "unknown";
    }

    private static void OpenContainingFolder(string filePath)
    {
        try
        {
            var folderPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(folderPath))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"-R \"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", folderPath);
            }
        }
        catch
        {
            // Silently fail - opening folder is not critical
        }
    }
}
