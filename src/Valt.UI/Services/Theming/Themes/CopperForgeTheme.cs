namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Copper Forge theme - Metallic copper/bronze accent dark theme
/// </summary>
public static class CopperForgeTheme
{
    public static ThemePalette Palette => new(
        "CopperForge",
        "Dark",
        new ColorScale("#F5D6C6", "#E8B89E", "#D4936F", "#C07848", "#B87333", "#9A5F2A", "#7C4B22", "#5E3918", "#402710"),
        new ColorScale("#A9E9DE", "#7ADACE", "#4BC6BB", "#35A9A3", "#2E8F8D", "#2A7474", "#26595C", "#214A4E", "#1C3D40"),
        new ColorScale("#FAF5F0", "#F5EBE2", "#E8DCD0", "#D4C4B4", "#B8A494", "#8A7868", "#5C504A", "#3A3230", "#1E1A18"),
        new ColorScale("#8A7468", "#78645A", "#68564C", "#584840", "#4A3C36", "#3E322C", "#342824", "#2A201C", "#1E1614"),
        new ColorScale("#8A7468", "#78645A", "#68564C", "#584840", "#4A3C36", "#3E322C", "#342824", "#2A201C", "#1E1614"),
        new ColorScale("#F5EBE2", "#E8DCD0", "#D4C0A8", "#B89878", "#8A6850", "#5C4438", "#3A2C24", "#281E18", "#180E0C"),
        new ColorScale("#B2F5EA", "#81E6D9", "#4FD1C5", "#38B2AC", "#319795", "#2C7A7B", "#285E61", "#234E52", "#1D4044"),
        new ColorScale("#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E", "#082F49"),
        new ColorScale("#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D"),
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#3B0764"),
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ThemeSpecificColors(
            "#180E0C", "#FAF5F0", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#2A201C", "#B87333", "#4A3C36", "#342824",
            "#40FFFFFF", "#26FFFFFF", "#C07848", "#68564C", "#8A7468",
            "#38BDF8", "#FBBF24", "#F87171", "#A855F7", "#C07848",
            "#3E322C", "#402710", "#5E3918", "#C07848", "#D4936F",
            "#FAF5F0", "#1E1614", "#FAF5F0"
        )
    );
}
