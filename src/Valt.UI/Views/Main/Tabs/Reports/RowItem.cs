using Avalonia.Controls;

namespace Valt.UI.Views.Main.Tabs.Reports;

public record RowItem(string LeftText, string RightText, Control? Tooltip = null, string? Url = null, bool IsSeparator = false)
{
    public bool HasUrl => !string.IsNullOrEmpty(Url);

    public static RowItem Separator() => new(string.Empty, string.Empty, IsSeparator: true);
}
