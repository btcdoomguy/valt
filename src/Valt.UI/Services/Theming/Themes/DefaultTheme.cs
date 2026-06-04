namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Default theme - Orange accent dark theme
/// </summary>
public static class DefaultTheme
{
    public static ThemePalette Palette => new(
        "Default",
        "Dark",
        new ColorScale("#ffe2bb", "#ffcc88", "#ffb755", "#ffa122", "#e98805", "#b26a09", "#7e4d0a", "#4d3008", "#1e1304"),
        new ColorScale("#bbd8ff", "#88bbff", "#559dff", "#2280ff", "#0566e9", "#0951b2", "#0a3b7e", "#08254d", "#040f1e"),
        new ColorScale("#fdfdfc", "#eeebe8", "#cfccc9", "#a8a6a4", "#83807c", "#6b6761", "#544e45", "#3b342b", "#1f1a14"),
        new ColorScale("#fcfcfc", "#ebebeb", "#cccccc", "#a6a6a6", "#808080", "#666666", "#414141", "#333333", "#1a1a1a"),
        new ColorScale("#fcfcfc", "#ebebeb", "#cccccc", "#a6a6a6", "#808080", "#666666", "#4d4d4d", "#333333", "#1a1a1a"),
        new ColorScale("#fdfdfc", "#eeebe8", "#cfccc9", "#a8a6a4", "#83807c", "#6b6761", "#544e45", "#3b342b", "#1f1a14"),
        new ColorScale("#bbd8ff", "#88bbff", "#559dff", "#2280ff", "#0566e9", "#0951b2", "#0a3b7e", "#08254d", "#040f1e"),
        new ColorScale("#8888ff", "#5555ff", "#2222ff", "#0606e8", "#0909b2", "#0a0a7e", "#08084d", "#04041e", "#000000"),
        new ColorScale("#ff8888", "#ff5555", "#ff2222", "#e80606", "#b20909", "#7e0a0a", "#4d0808", "#1e0404", "#000000"),
        new ColorScale("#88ff88", "#55ff55", "#22ff22", "#06e806", "#09b209", "#0a7e0a", "#084d08", "#041e04", "#000000"),
        new ColorScale("#ff88ff", "#ff55ff", "#ff22ff", "#e806e8", "#b209b2", "#7e0a7e", "#4d084d", "#1e041e", "#000000"),
        new ColorScale("#fbffd5", "#f6ffa2", "#f1ff6f", "#eafb40", "#dff414", "#b5c60f", "#86930f", "#5a620d", "#2f3309"),
        new ThemeSpecificColors(
            "#1f1a14", "#ffffff", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#0D0D0D", "#2A2A2A", "#1A1A1A", "#1A1A1A",
            "#69FFFFFF", "#49FFFFFF", "#0078D4", "#6B6B6B", "#9E9E9E",
            "#2196F3", "#FF9800", "#F44336", "#9C27B0", "#FFE98805",
            "#444444", "#5B3800", "#7A5500", "#FF9E00", "#FFA500",
            "#FFFFFF", "#000000", "#FFFFFF"
        )
    );
}
