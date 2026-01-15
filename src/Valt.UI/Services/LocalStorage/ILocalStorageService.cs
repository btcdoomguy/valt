using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Valt.UI.Services.LocalStorage;

public interface ILocalStorageService
{
    List<string> LoadRecentFiles();
    Task ChangeRecentFilesAsync(ICollection<string> recentFiles);
    string LoadCulture();
    Task ChangeCultureAsync(string culture);
    string LoadTheme();
    Task ChangeThemeAsync(string theme);
    DataGridSettings LoadDataGridSettings();
    Task SaveDataGridSettingsAsync(DataGridSettings settings);
}

public class DataGridSettings
{
    public Dictionary<string, double> ColumnWidths { get; set; } = new();
    public List<string> ColumnOrder { get; set; } = new();
    public string? OrderedColumn { get; set; }
    public ListSortDirection? SortDirection { get; set; }
}
