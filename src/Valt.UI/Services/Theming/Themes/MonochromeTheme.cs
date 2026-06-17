namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Monochrome theme - Pure black, white, and gray tones only
/// </summary>
public static class MonochromeTheme
{
    public static ThemePalette Palette => new(
        "Monochrome",
        "Dark",
        new ColorScale("#FFFFFF", "#E5E5E5", "#CCCCCC", "#B3B3B3", "#999999", "#808080", "#666666", "#4D4D4D", "#333333"),
        new ColorScale("#F5F5F5", "#E0E0E0", "#BDBDBD", "#9E9E9E", "#757575", "#616161", "#424242", "#303030", "#212121"),
        new ColorScale("#FFFFFF", "#FAFAFA", "#E0E0E0", "#BDBDBD", "#9E9E9E", "#757575", "#616161", "#424242", "#212121"),
        new ColorScale("#8F8F8F", "#7A7A7A", "#686868", "#565656", "#484848", "#3C3C3C", "#323232", "#2A2A2A", "#1E1E1E"),
        new ColorScale("#8F8F8F", "#7A7A7A", "#686868", "#565656", "#484848", "#3C3C3C", "#323232", "#2A2A2A", "#1E1E1E"),
        new ColorScale("#FFFFFF", "#E0E0E0", "#BDBDBD", "#9E9E9E", "#808080", "#5E5E5E", "#454545", "#333333", "#1A1A1A"),
        new ColorScale("#FFFFFF", "#E5E5E5", "#CCCCCC", "#B3B3B3", "#999999", "#808080", "#666666", "#4D4D4D", "#333333"),
        new ColorScale("#E0E0E0", "#BDBDBD", "#9E9E9E", "#858585", "#6E6E6E", "#575757", "#424242", "#303030", "#1A1A1A"),
        new ColorScale("#D4D4D4", "#A3A3A3", "#737373", "#525252", "#404040", "#303030", "#262626", "#1A1A1A", "#0F0F0F"),
        new ColorScale("#FAFAFA", "#F0F0F0", "#E0E0E0", "#D0D0D0", "#B8B8B8", "#A0A0A0", "#888888", "#707070", "#585858"),
        new ColorScale("#E8E8E8", "#D0D0D0", "#B0B0B0", "#909090", "#787878", "#606060", "#484848", "#383838", "#282828"),
        new ColorScale("#F5F5F5", "#E5E5E5", "#D5D5D5", "#C0C0C0", "#A8A8A8", "#909090", "#787878", "#606060", "#484848"),
        new ThemeSpecificColors(
            "#000000", "#FFFFFF", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#2A2A2A", "#9E9E9E", "#484848", "#323232",
            "#50FFFFFF", "#30FFFFFF", "#FFFFFF", "#686868", "#8F8F8F",
            "#BDBDBD", "#E0E0E0", "#808080", "#B0B0B0", "#FFFFFF",
            "#3C3C3C", "#484848", "#686868", "#FFFFFF", "#E0E0E0",
            "#FFFFFF", "#1E1E1E", "#FFFFFF"
        )
    );
}
