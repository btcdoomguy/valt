namespace Valt.UI.Views.Main.Tabs.Reports;

public record RowItem(string LeftText, string RightText, string? Tooltip = null, string? Url = null)
{
    public bool HasUrl => !string.IsNullOrEmpty(Url);
}
