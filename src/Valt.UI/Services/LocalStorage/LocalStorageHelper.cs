using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Valt.Infra.Kernel;

namespace Valt.UI.Services.LocalStorage;

public static class LocalStorageHelper
{
    private static ValtSettings? _valtSettings;

    private static readonly string ValtSettingsFilePath = Path.Combine(
        ValtEnvironment.AppDataPath,
        "valt_settings.json");

    public static List<string> LoadRecentFiles()
    {
        if (_valtSettings is null)
            Load();

        return _valtSettings!.RecentFiles;
    }

    public static Task ChangeRecentFiles(ICollection<string> recentFiles)
    {
        if (_valtSettings is null)
            Load();

        _valtSettings!.RecentFiles = recentFiles.ToList();
        
        Save();
        
        return Task.CompletedTask;
    }
    
    public static string LoadCulture()
    {
        if (_valtSettings is null)
            Load();
        
        return _valtSettings!.Culture;
    }

    public static Task ChangeCulture(string culture)
    {
        if (_valtSettings is null)
            Load();
        
        _valtSettings!.Culture = culture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(LoadCulture());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(LoadCulture());
        
        Save();
        
        return Task.CompletedTask;
    }

    public static void ChangeDataGridSettings(DataGrid dataGrid, string? orderedColumn,
        ListSortDirection? sortDirection)
    {
        if (_valtSettings is null)
            Load();

        var settings = new DataGridColumnSettings();
        foreach (var column in dataGrid.Columns)
        {
            var columnId = column.Header?.ToString();
            if (!string.IsNullOrEmpty(columnId))
            {
                settings.ColumnWidths[columnId] = column.Width.DisplayValue;
            }
        }

        var orderedColumns = dataGrid.Columns.OrderBy(c => c.DisplayIndex).ToList();
        settings.ColumnOrder = orderedColumns
            .Where(c => c.Header is not null)
            .Select(c => c.Header.ToString()!)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        if (orderedColumn is not null)
        {
            settings.OrderedColumn = orderedColumn;
            settings.SortDirection = sortDirection ?? ListSortDirection.Ascending;
        }

        _valtSettings!.DataGridSettings = settings;

        Save();
    }

    public static DataGridColumnSettings LoadDataGridSettings()
    {
        if (_valtSettings is null)
            Load();

        return _valtSettings!.DataGridSettings;
    }

    private static void Load()
    {
        if (!File.Exists(ValtSettingsFilePath))
        {
            _valtSettings = new ValtSettings();
            return;
        }

        var json = File.ReadAllText(ValtSettingsFilePath);
        var data = JsonSerializer.Deserialize<ValtSettings>(json);

        _valtSettings = data ?? new ValtSettings();
    }

    private static void Save()
    {
        var json = JsonSerializer.Serialize(_valtSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ValtSettingsFilePath, json);
    }

    public class ValtSettings
    {
        public string Culture { get; set; } = CultureInfo.CurrentCulture.Name;
        public DataGridColumnSettings DataGridSettings { get; set; } = new();
        public List<string> RecentFiles { get; set; } = new();
    }

    public class DataGridColumnSettings
    {
        public Dictionary<string, double> ColumnWidths { get; set; } = new();
        public List<string> ColumnOrder { get; set; } = new();
        public string? OrderedColumn { get; set; }
        public ListSortDirection? SortDirection { get; set; }
    }
}