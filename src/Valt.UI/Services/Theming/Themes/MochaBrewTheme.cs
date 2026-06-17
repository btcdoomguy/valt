namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Mocha Brew theme - Rich coffee/mocha/chocolate accent dark theme
/// </summary>
public static class MochaBrewTheme
{
    public static ThemePalette Palette => new(
        "MochaBrew",
        "Dark",
        new ColorScale("#E7D4C0", "#D4BC9E", "#C4A882", "#A8876A", "#8B6F55", "#6F5842", "#574533", "#3F3225", "#2A2119"),
        new ColorScale("#C2EEE4", "#91E9D8", "#59DDC9", "#2BC9B5", "#13AF9E", "#0C8D81", "#0E7068", "#105954", "#124A46"),
        new ColorScale("#FAF7F2", "#F5EFE6", "#E8DFD2", "#D4C8B8", "#B8A898", "#8A7A6A", "#5C5048", "#3A3230", "#1E1A18"),
        new ColorScale("#8A7868", "#78685A", "#68584C", "#584A40", "#4A3E36", "#3E342C", "#342A24", "#2A221C", "#1E1814"),
        new ColorScale("#8A7868", "#78685A", "#68584C", "#584A40", "#4A3E36", "#3E342C", "#342A24", "#2A221C", "#1E1814"),
        new ColorScale("#F5EFE6", "#E8DFD2", "#D4C8B0", "#B8A088", "#8A7260", "#5C4C40", "#3A3028", "#28201A", "#18120E"),
        new ColorScale("#CCFBF1", "#99F6E4", "#5EEAD4", "#2DD4BF", "#14B8A6", "#0D9488", "#0F766E", "#115E59", "#134E4A"),
        new ColorScale("#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E", "#082F49"),
        new ColorScale("#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D"),
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#3B0764"),
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ThemeSpecificColors(
            "#18120E", "#FAF7F2", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#2A221C", "#8B6F55", "#4A3E36", "#342A24",
            "#40FFFFFF", "#26FFFFFF", "#A8876A", "#68584C", "#8A7868",
            "#38BDF8", "#FBBF24", "#F87171", "#A855F7", "#A8876A",
            "#3E342C", "#2A2119", "#3F3225", "#A8876A", "#C4A882",
            "#FAF7F2", "#1E1814", "#FAF7F2"
        )
    );
}
