using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Valt.Infra.Kernel;

namespace Valt.UI.Services.LocalStorage;

public class LocalStorageService : ILocalStorageService
{
    private ValtSettings? _valtSettings;
    private readonly string _settingsFilePath;

    public LocalStorageService()
    {
        _settingsFilePath = Path.Combine(ValtEnvironment.AppDataPath, "valt_settings.json");
    }

    public LocalStorageService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public List<string> LoadRecentFiles()
    {
        EnsureLoaded();
        return _valtSettings!.RecentFiles;
    }

    public Task ChangeRecentFilesAsync(ICollection<string> recentFiles)
    {
        EnsureLoaded();
        _valtSettings!.RecentFiles = recentFiles.ToList();
        Save();
        return Task.CompletedTask;
    }

    public string LoadCulture()
    {
        EnsureLoaded();
        return _valtSettings!.Culture;
    }

    public Task ChangeCultureAsync(string culture)
    {
        EnsureLoaded();
        _valtSettings!.Culture = culture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        Save();
        return Task.CompletedTask;
    }

    public string LoadTheme()
    {
        EnsureLoaded();
        return _valtSettings!.Theme;
    }

    public Task ChangeThemeAsync(string theme)
    {
        EnsureLoaded();
        _valtSettings!.Theme = theme;
        Save();
        return Task.CompletedTask;
    }

    public DataGridSettings LoadDataGridSettings()
    {
        EnsureLoaded();
        return _valtSettings!.DataGridSettings;
    }

    public Task SaveDataGridSettingsAsync(DataGridSettings settings)
    {
        EnsureLoaded();
        _valtSettings!.DataGridSettings = settings;
        Save();
        return Task.CompletedTask;
    }

    private void EnsureLoaded()
    {
        if (_valtSettings is not null) return;

        if (!File.Exists(_settingsFilePath))
        {
            _valtSettings = new ValtSettings();
            return;
        }

        var json = File.ReadAllText(_settingsFilePath);
        var data = JsonSerializer.Deserialize<ValtSettings>(json);
        _valtSettings = data ?? new ValtSettings();
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_valtSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, json);
    }

    private class ValtSettings
    {
        public string Culture { get; set; } = GetDefaultCulture();
        public string Theme { get; set; } = "Dark";
        public DataGridSettings DataGridSettings { get; set; } = new();
        public List<string> RecentFiles { get; set; } = new();

        private static string GetDefaultCulture()
        {
            var osCulture = CultureInfo.CurrentCulture;

            // Check if OS culture is Portuguese (any variant) and map to pt-BR
            if (osCulture.TwoLetterISOLanguageName.Equals("pt", System.StringComparison.OrdinalIgnoreCase))
            {
                return "pt-BR";
            }

            // Default to en-US for all other languages
            return "en-US";
        }
    }
}
