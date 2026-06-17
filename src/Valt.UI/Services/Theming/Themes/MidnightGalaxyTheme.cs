namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Midnight Galaxy theme - Deep purple/violet cosmic dark theme
/// </summary>
public static class MidnightGalaxyTheme
{
    public static ThemePalette Palette => new(
        "MidnightGalaxy",
        "Dark",
        new ColorScale("#E9D8FD", "#D6BCFA", "#B794F4", "#9F7AEA", "#805AD5", "#6B46C1", "#553C9A", "#44337A", "#322659"),
        new ColorScale("#F1E7BC", "#F0DA83", "#EFC849", "#EEB522", "#E9960A", "#CE7106", "#AB4F09", "#8B3D0D", "#72320E"),
        new ColorScale("#FAF5FF", "#E9E3F0", "#D6CFE2", "#B8B0C8", "#9590A8", "#706B85", "#524D66", "#363245", "#1E1B2A"),
        new ColorScale("#8A7BAA", "#786898", "#665A85", "#564B72", "#483E62", "#3C3155", "#332848", "#2B2140", "#1E1530"),
        new ColorScale("#8A7BAA", "#786898", "#665A85", "#564B72", "#483E62", "#3C3155", "#332848", "#2B2140", "#1E1530"),
        new ColorScale("#E9E3F0", "#D6CFE2", "#B8B0C8", "#9590A8", "#706B85", "#524D66", "#363245", "#252035", "#14111E"),
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ColorScale("#A3BFFA", "#7F9CF5", "#667EEA", "#5A67D8", "#4C51BF", "#434190", "#3C366B", "#2D2A5C", "#1A1744"),
        new ColorScale("#FED7E2", "#FBB6CE", "#F687B3", "#ED64A6", "#D53F8C", "#B83280", "#97266D", "#702459", "#4A1942"),
        new ColorScale("#9AE6B4", "#68D391", "#48BB78", "#38A169", "#2F855A", "#276749", "#22543D", "#1C4532", "#0D2818"),
        new ColorScale("#B2F5EA", "#81E6D9", "#4FD1C5", "#38B2AC", "#319795", "#2C7A7B", "#285E61", "#234E52", "#1D4044"),
        new ColorScale("#FAF089", "#F6E05E", "#ECC94B", "#D69E2E", "#B7791F", "#975A16", "#744210", "#5F370E", "#3D2508"),
        new ThemeSpecificColors(
            "#08060C", "#FAF5FF", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#2B2140", "#6B46C1", "#483E62", "#332848",
            "#40FFFFFF", "#26FFFFFF", "#805AD5", "#665A85", "#8A7BAA",
            "#667EEA", "#D69E2E", "#ED64A6", "#38B2AC", "#9F7AEA",
            "#3C3155", "#322659", "#44337A", "#9F7AEA", "#B794F4",
            "#FAF5FF", "#1E1530", "#FAF5FF"
        )
    );
}
