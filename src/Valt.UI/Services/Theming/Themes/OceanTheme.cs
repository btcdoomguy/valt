namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Ocean theme - Teal/Cyan accent dark theme
/// </summary>
public static class OceanTheme
{
    public static ThemePalette Palette => new(
        "Ocean",
        "Dark",
        new ColorScale("#B2F5EA", "#81E6D9", "#4FD1C5", "#38B2AC", "#319795", "#2C7A7B", "#285E61", "#234E52", "#1D4044"),
        new ColorScale("#F1CCCC", "#F1A9A9", "#EF7A7A", "#E86060", "#D93B3B", "#BB2D2D", "#932A2A", "#7B2525", "#5E1619"),
        new ColorScale("#F7FAFC", "#EDF2F7", "#E2E8F0", "#CBD5E0", "#A0AEC0", "#718096", "#4A5568", "#2D3748", "#1A202C"),
        new ColorScale("#6B9494", "#5B8282", "#4D6F6F", "#405C5C", "#364D4D", "#2D4040", "#263636", "#1F3333", "#152424"),
        new ColorScale("#6B9494", "#5B8282", "#4D6F6F", "#405C5C", "#364D4D", "#2D4040", "#263636", "#1F3333", "#152424"),
        new ColorScale("#E2E8F0", "#CBD5E0", "#A0AEC0", "#718096", "#4A5568", "#2D3748", "#1A202C", "#171923", "#0D1117"),
        new ColorScale("#FED7D7", "#FEB2B2", "#FC8181", "#F56565", "#E53E3E", "#C53030", "#9B2C2C", "#822727", "#63171B"),
        new ColorScale("#90CDF4", "#63B3ED", "#4299E1", "#3182CE", "#2B6CB0", "#2C5282", "#2A4365", "#1A365D", "#0D1B2A"),
        new ColorScale("#FEB2B2", "#FC8181", "#F56565", "#E53E3E", "#C53030", "#9B2C2C", "#742A2A", "#4A1F1F", "#1A0A0A"),
        new ColorScale("#9AE6B4", "#68D391", "#48BB78", "#38A169", "#2F855A", "#276749", "#22543D", "#1C4532", "#0D2818"),
        new ColorScale("#D6BCFA", "#B794F4", "#9F7AEA", "#805AD5", "#6B46C1", "#553C9A", "#44337A", "#322659", "#1A1033"),
        new ColorScale("#FAF089", "#F6E05E", "#ECC94B", "#D69E2E", "#B7791F", "#975A16", "#744210", "#5F370E", "#3D2508"),
        new ThemeSpecificColors(
            "#0D1117", "#F7FAFC", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#1F3333", "#2C7A7B", "#364D4D", "#263636",
            "#40FFFFFF", "#26FFFFFF", "#319795", "#4D6F6F", "#6B9494",
            "#4299E1", "#D69E2E", "#E53E3E", "#805AD5", "#38B2AC",
            "#2D4040", "#1D4044", "#285E61", "#38B2AC", "#4FD1C5",
            "#F7FAFC", "#152424", "#F7FAFC"
        )
    );
}
