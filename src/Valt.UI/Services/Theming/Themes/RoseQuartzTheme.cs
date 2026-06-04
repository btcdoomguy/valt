namespace Valt.UI.Services.Theming.Themes;

/// <summary>
/// Rose Quartz theme - Soft pink/rose/magenta accent dark theme
/// </summary>
public static class RoseQuartzTheme
{
    public static ThemePalette Palette => new(
        "RoseQuartz",
        "Dark",
        new ColorScale("#FECDD3", "#FBB6C4", "#F687A5", "#EC4889", "#DB2777", "#BE185D", "#9D174D", "#831843", "#500724"),
        new ColorScale("#C2EEE4", "#91E9D8", "#59DDC9", "#2BC9B5", "#13AF9E", "#0C8D81", "#0E7068", "#105954", "#124A46"),
        new ColorScale("#FFF1F2", "#FFE4E6", "#FECDD3", "#E8BDC2", "#C9A0A8", "#9A7580", "#6B5058", "#3D2E32", "#1F1718"),
        new ColorScale("#9A7A85", "#886878", "#765868", "#644858", "#543C4A", "#46323E", "#3A2832", "#302028", "#22161C"),
        new ColorScale("#9A7A85", "#886878", "#765868", "#644858", "#543C4A", "#46323E", "#3A2832", "#302028", "#22161C"),
        new ColorScale("#FFE4E6", "#FECDD3", "#E8B0B8", "#B8808A", "#8A5A62", "#5C3A40", "#3A2428", "#281A1E", "#180E12"),
        new ColorScale("#CCFBF1", "#99F6E4", "#5EEAD4", "#2DD4BF", "#14B8A6", "#0D9488", "#0F766E", "#115E59", "#134E4A"),
        new ColorScale("#BAE6FD", "#7DD3FC", "#38BDF8", "#0EA5E9", "#0284C7", "#0369A1", "#075985", "#0C4A6E", "#082F49"),
        new ColorScale("#FEE2E2", "#FECACA", "#FCA5A5", "#F87171", "#EF4444", "#DC2626", "#B91C1C", "#991B1B", "#7F1D1D"),
        new ColorScale("#D1FAE5", "#A7F3D0", "#6EE7B7", "#34D399", "#10B981", "#059669", "#047857", "#065F46", "#064E3B"),
        new ColorScale("#E9D5FF", "#D8B4FE", "#C084FC", "#A855F7", "#9333EA", "#7E22CE", "#6B21A8", "#581C87", "#3B0764"),
        new ColorScale("#FEF3C7", "#FDE68A", "#FCD34D", "#FBBF24", "#F59E0B", "#D97706", "#B45309", "#92400E", "#78350F"),
        new ThemeSpecificColors(
            "#180E12", "#FFF1F2", "#78DB55", "#FF7D7D", "#FFF866",
            "#FFFFFF", "#302028", "#DB2777", "#543C4A", "#3A2832",
            "#40FFFFFF", "#26FFFFFF", "#EC4889", "#765868", "#9A7A85",
            "#38BDF8", "#FBBF24", "#F87171", "#A855F7", "#EC4889",
            "#46323E", "#500724", "#831843", "#EC4889", "#F687A5",
            "#FFF1F2", "#22161C", "#FFF1F2"
        )
    );
}
