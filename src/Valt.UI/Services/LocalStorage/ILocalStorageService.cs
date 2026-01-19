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
    string LoadFontScale();
    Task ChangeFontScaleAsync(string fontScale);
    DataGridSettings LoadDataGridSettings();
    Task SaveDataGridSettingsAsync(DataGridSettings settings);
    LayoutSettings LoadLayoutSettings();
    Task SaveLayoutSettingsAsync(LayoutSettings settings);
    WindowSettings LoadWindowSettings();
    Task SaveWindowSettingsAsync(WindowSettings settings);
}

public class DataGridSettings
{
    public Dictionary<string, double> ColumnWidths { get; set; } = new();
    public List<string> ColumnOrder { get; set; } = new();
    public string? OrderedColumn { get; set; }
    public ListSortDirection? SortDirection { get; set; }
}

public class LayoutSettings
{
    public double? RightPanelWidth { get; set; }
    public double? FixedExpensesPanelHeight { get; set; }
}

public class WindowSettings
{
    public bool IsMaximized { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
}
